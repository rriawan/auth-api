using AuthApi.Application.Common.Interfaces;
using AuthApi.Domain.Entities;
using AuthApi.Domain.Entities.Organization;
using AuthApi.Domain.Entities.Employees;
using Microsoft.EntityFrameworkCore;

namespace AuthApi.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options), IAppDbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<ClientApp> ClientApps => Set<ClientApp>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<UserAppRole> UserAppRoles => Set<UserAppRole>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<ActionToken> ActionTokens => Set<ActionToken>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<OfficeClassification> OfficeClassifications => Set<OfficeClassification>();
    public DbSet<Office> Offices => Set<Office>();
    public DbSet<OfficeVersion> OfficeVersions => Set<OfficeVersion>();
    public DbSet<OrgUnit> OrgUnits => Set<OrgUnit>();
    public DbSet<OrgUnitVersion> OrgUnitVersions => Set<OrgUnitVersion>();
    public DbSet<PositionLevel> PositionLevels => Set<PositionLevel>();
    public DbSet<Position> Positions => Set<Position>();
    public DbSet<PositionVersion> PositionVersions => Set<PositionVersion>();
    public DbSet<StructureTemplate> StructureTemplates => Set<StructureTemplate>();
    public DbSet<TemplateOrgUnit> TemplateOrgUnits => Set<TemplateOrgUnit>();
    public DbSet<TemplatePosition> TemplatePositions => Set<TemplatePosition>();
    public DbSet<OfficeTemplateApplication> OfficeTemplateApplications => Set<OfficeTemplateApplication>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<EmployeeStatus> EmployeeStatuses => Set<EmployeeStatus>();
    public DbSet<EmployeePosition> EmployeePositions => Set<EmployeePosition>();
    public DbSet<EmployeeActingAssignment> EmployeeActingAssignments => Set<EmployeeActingAssignment>();
    public DbSet<UserEmployeeLink> UserEmployeeLinks => Set<UserEmployeeLink>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}

