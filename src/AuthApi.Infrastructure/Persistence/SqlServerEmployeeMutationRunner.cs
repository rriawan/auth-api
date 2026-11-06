using System.Data;
using AuthApi.Application.Common.Interfaces;
using AuthApi.Domain.Entities.Employees;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace AuthApi.Infrastructure.Persistence;

/// <summary>
/// Conservative initial serialization for low-volume organization administration.
/// Future organization writers must share this transaction-owned module lock.
/// No connection is opened by construction or DI registration.
/// </summary>
public sealed class SqlServerEmployeeMutationRunner(AppDbContext db) : IEmployeeMutationRunner
{
    public bool IsActive { get; private set; }

    public void MarkEmployeeChanged(Employee employee)
    {
        if (!IsActive) throw new InvalidOperationException("Employee writes require an active mutation transaction.");
        var entry = db.Entry(employee);
        if (entry.State == EntityState.Detached || entry.State == EntityState.Deleted)
            throw new InvalidOperationException("The employee must be tracked in the mutation context.");
        if (entry.State != EntityState.Added)
            entry.Property(x => x.UpdatedAtUtc).IsModified = true;
    }

    public void MarkMasterChanged(object master)
    {
        if (!IsActive) throw new InvalidOperationException("Master data writes require an active mutation transaction.");
        if (master is not (Domain.Entities.Organization.Office or Domain.Entities.Organization.OrgUnit or Domain.Entities.Organization.Position))
            throw new ArgumentException("Only office, unit and position aggregates carry a master rowversion.", nameof(master));
        var entry = db.Entry(master);
        if (entry.State is EntityState.Detached or EntityState.Deleted)
            throw new InvalidOperationException("The master record must be tracked in the mutation context.");
        if (entry.State != EntityState.Added) entry.Property("CreatedAtUtc").IsModified = true;
    }

    public async Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(action);
        if (IsActive || db.Database.CurrentTransaction is not null || System.Transactions.Transaction.Current is not null)
            throw new InvalidOperationException("Nested or ambient employee mutation transactions are not supported.");
        if (!db.Database.IsSqlServer())
            throw new InvalidOperationException("The production mutation runner requires SQL Server.");
        if (db.ChangeTracker.HasChanges())
            throw new InvalidOperationException("Start employee mutations with a clean scoped context.");

        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        try
        {
            await using var command = db.Database.GetDbConnection().CreateCommand();
            command.Transaction = transaction.GetDbTransaction();
            command.CommandTimeout = 10;
            command.CommandText = """
                DECLARE @result int;
                EXEC @result = sys.sp_getapplock @Resource = @Resource,
                    @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 5000;
                SELECT @result;
                """;
            var resource = command.CreateParameter();
            resource.ParameterName = "@Resource";
            resource.DbType = DbType.String;
            resource.Size = 255;
            resource.Value = "auth-api:organization-employee";
            command.Parameters.Add(resource);
            var result = await command.ExecuteScalarAsync(cancellationToken);
            if (result is not int code || code < 0)
                throw new InvalidOperationException("The organization mutation lock could not be acquired; retry with a fresh version.");

            IsActive = true;
            var value = await action(cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return value;
        }
        catch
        {
            try { await transaction.RollbackAsync(CancellationToken.None); }
            finally { db.ChangeTracker.Clear(); }
            throw;
        }
        finally
        {
            IsActive = false;
        }
    }
}
