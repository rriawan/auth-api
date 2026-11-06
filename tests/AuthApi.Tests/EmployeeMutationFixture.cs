using AuthApi.Application.Common.Interfaces;
using AuthApi.Application.Features.Employees;
using AuthApi.Domain.Constants;
using AuthApi.Domain.Entities;
using AuthApi.Domain.Entities.Employees;
using AuthApi.Domain.Entities.Organization;
using AuthApi.Domain.Enums;
using AuthApi.Domain.Enums.Organization;
using AuthApi.Infrastructure.Persistence;
using AuthApi.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace AuthApi.Tests;

/// <summary>Memory-only command fixture; the transaction double does not prove SQL locking/rollback.</summary>
public sealed class EmployeeMutationFixture : IDisposable
{
    private readonly MemoryRowVersions _versions = new();
    private readonly TimeProvider _clock = new FixedEmployeeClock();
    private Guid? _unitId;
    public AppDbContext Db { get; }
    public MemoryEmployeeMutationRunner Runner { get; }
    public EmployeeMutationSupport Support { get; }
    public EmployeeProfileService Profiles { get; }
    public EmployeePlacementService Placements { get; }
    public EmployeeActor Actor { get; } = new(Guid.NewGuid(), "127.0.0.1", "memory-unit-test");
    public DateOnly Today { get; } = new(2026, 11, 6);
    public bool FailAudit { get; set; }

    private EmployeeMutationFixture()
    {
        Db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("employee-commands-" + Guid.NewGuid().ToString("N"))
            .AddInterceptors(_versions).Options);
        Runner = new MemoryEmployeeMutationRunner(Db, _versions);
        Support = new EmployeeMutationSupport(Db, Runner,
            new ConfigurableAudit(new AuditLogService(Db), () => FailAudit), _clock);
        Placements = new EmployeePlacementService(Db, Runner, Support);
        Profiles = new EmployeeProfileService(Db, Runner, Support, Placements);
    }

    public static async Task<EmployeeMutationFixture> CreateAsync()
    {
        var fixture = new EmployeeMutationFixture();
        var app = new ClientApp { ClientId = SystemConstants.DefaultAdminClientId, Name = "Test administration", ApiKeyHash = new string('0', 64), ApiKeyPrefix = "unused", IsActive = true };
        var user = new User { Id = fixture.Actor.UserId, Username = "admin-p2b", Email = "admin@example.test", FullName = "Test Admin", PasswordHash = "test-hash-unused", IsActive = true, IsAccountVerified = true };
        var role = new Role { ClientAppId = app.Id, Name = SystemConstants.DefaultSuperAdminRole, IsSystemRole = true };
        fixture.Db.ClientApps.Add(app);
        fixture.Db.Users.Add(user);
        fixture.Db.Roles.Add(role);
        fixture.Db.UserAppRoles.Add(new UserAppRole { UserId = user.Id, ClientAppId = app.Id, RoleId = role.Id });
        await fixture.Db.SaveChangesAsync();
        return fixture;
    }

    public async Task<EmployeeSummary> CreateEmployeeAsync(string number = "NIP-001", string name = "Test Employee", DateOnly? from = null) =>
        (await Profiles.CreateAsync(new CreateEmployeeCommand(number, name, from ?? Today, "Test employee creation"), Actor)).Employee;

    public string Version(Guid employeeId) => '"' + Convert.ToBase64String(Db.Employees.AsNoTracking().Single(x => x.Id == employeeId).RowVersion) + '"';

    public async Task<Guid> AddPositionAsync(string code, int? capacity = null)
    {
        if (_unitId is null)
        {
            var classification = new OfficeClassification { Code = "CAB-I", DisplayName = "Test classification" };
            var office = new Office { OfficeCode = "220", CreatedByUserId = Actor.UserId };
            var unit = new OrgUnit { OfficeId = office.Id, UnitCode = "ROOT", CreatedByUserId = Actor.UserId };
            Db.OfficeClassifications.Add(classification);
            Db.Offices.Add(office);
            Db.OfficeVersions.Add(new OfficeVersion { OfficeId = office.Id, ClassificationCode = classification.Code, Name = "Test Office", EffectiveFrom = Today, ChangeReason = "Test office", CreatedByUserId = Actor.UserId });
            Db.OrgUnits.Add(unit);
            Db.OrgUnitVersions.Add(new OrgUnitVersion { OrgUnitId = unit.Id, UnitType = OrgUnitType.Other, Name = "Test Unit", EffectiveFrom = Today, ChangeReason = "Test unit", CreatedByUserId = Actor.UserId });
            _unitId = unit.Id;
        }

        var position = new Position { OrgUnitId = _unitId.Value, PositionCode = code, CreatedByUserId = Actor.UserId };
        Db.Positions.Add(position);
        Db.PositionVersions.Add(new PositionVersion { PositionId = position.Id, Name = code, Track = PositionTrack.Line,
            IsUnitHead = capacity == 1, MaxIncumbents = capacity, EffectiveFrom = Today, ChangeReason = "Test position", CreatedByUserId = Actor.UserId });
        await Db.SaveChangesAsync();
        return position.Id;
    }

    public void Dispose() => Db.Dispose();

    private sealed class FixedEmployeeClock : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(2026, 11, 6, 4, 0, 0, TimeSpan.Zero);
    }

    private sealed class ConfigurableAudit(IAuditLogService inner, Func<bool> fail) : IAuditLogService
    {
        public Task LogAsync(AuditEventType eventType, bool isSuccess, Guid? userId = null, Guid? clientAppId = null,
            string? ipAddress = null, string? userAgent = null, object? detail = null, CancellationToken cancellationToken = default)
        {
            if (fail()) throw new InvalidOperationException("Injected audit failure.");
            return inner.LogAsync(eventType, isSuccess, userId, clientAppId, ipAddress, userAgent, detail, cancellationToken);
        }
    }

    public sealed class MemoryRowVersions : SaveChangesInterceptor
    {
        private long _sequence;
        public bool IsRestoring { get; set; }
        private void Assign(DbContext? context)
        {
            if (context is null || IsRestoring) return;
            foreach (var entry in context.ChangeTracker.Entries()
                .Where(x => x.Entity is Employee or Office or OrgUnit or Position or StructureTemplate && x.State is EntityState.Added or EntityState.Modified))
                entry.Property("RowVersion").CurrentValue = BitConverter.GetBytes(Interlocked.Increment(ref _sequence));
        }
        public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
        { Assign(eventData.Context); return result; }
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
            InterceptionResult<int> result, CancellationToken cancellationToken = default)
        { Assign(eventData.Context); return ValueTask.FromResult(result); }
    }

    /// <summary>Explicit snapshot rollback for memory tests, not a relational transaction.</summary>
    public sealed class MemoryEmployeeMutationRunner(AppDbContext db, MemoryRowVersions versions) : IEmployeeMutationRunner
    {
        public bool IsActive { get; private set; }
        public int CommitCount { get; private set; }
        public int RollbackCount { get; private set; }
        public void MarkMasterChanged(object master)
        {
            if (!IsActive) throw new InvalidOperationException("Missing test mutation boundary.");
            if (db.Entry(master).State != EntityState.Added) db.Entry(master).Property("CreatedAtUtc").IsModified = true;
        }

        public void MarkEmployeeChanged(Employee employee)
        {
            if (!IsActive) throw new InvalidOperationException("Missing test mutation boundary.");
            if (db.Entry(employee).State != EntityState.Added) db.Entry(employee).Property(x => x.UpdatedAtUtc).IsModified = true;
        }

        public async Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken = default)
        {
            if (IsActive || db.ChangeTracker.HasChanges()) throw new InvalidOperationException("Nested/dirty test mutation.");
            var employees = Capture(db.Employees);
            var statuses = Capture(db.EmployeeStatuses);
            var placements = Capture(db.EmployeePositions);
            var acting = Capture(db.EmployeeActingAssignments);
            var links = Capture(db.UserEmployeeLinks);
            var audit = Capture(db.AuditLogs);
            IsActive = true;
            try
            {
                var value = await action(cancellationToken);
                await db.SaveChangesAsync(cancellationToken);
                CommitCount++;
                return value;
            }
            catch
            {
                RollbackCount++;
                db.ChangeTracker.Clear();
                versions.IsRestoring = true;
                try
                {
                    db.UserEmployeeLinks.RemoveRange(db.UserEmployeeLinks.ToArray());
                    db.EmployeeActingAssignments.RemoveRange(db.EmployeeActingAssignments.ToArray());
                    db.EmployeePositions.RemoveRange(db.EmployeePositions.ToArray());
                    db.EmployeeStatuses.RemoveRange(db.EmployeeStatuses.ToArray());
                    db.Employees.RemoveRange(db.Employees.ToArray());
                    db.AuditLogs.RemoveRange(db.AuditLogs.ToArray());
                    await db.SaveChangesAsync(CancellationToken.None);
                    db.ChangeTracker.Clear();
                    db.Employees.AddRange(employees);
                    db.EmployeeStatuses.AddRange(statuses);
                    db.EmployeePositions.AddRange(placements);
                    db.EmployeeActingAssignments.AddRange(acting);
                    db.UserEmployeeLinks.AddRange(links);
                    db.AuditLogs.AddRange(audit);
                    await db.SaveChangesAsync(CancellationToken.None);
                    db.ChangeTracker.Clear();
                }
                finally { versions.IsRestoring = false; }
                throw;
            }
            finally { IsActive = false; }
        }

        private T[] Capture<T>(DbSet<T> set) where T : class, new() => set.AsNoTracking().ToArray().Select(entity =>
        {
            var clone = new T();
            foreach (var property in db.Model.FindEntityType(typeof(T))!.GetProperties())
            {
                var value = property.PropertyInfo!.GetValue(entity);
                property.PropertyInfo.SetValue(clone, value is byte[] bytes ? bytes.ToArray() : value);
            }
            return clone;
        }).ToArray();
    }
}
