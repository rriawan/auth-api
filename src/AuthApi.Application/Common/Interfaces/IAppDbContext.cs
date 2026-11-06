using AuthApi.Domain.Entities;
using AuthApi.Domain.Entities.Organization;
using AuthApi.Domain.Entities.Employees;
using Microsoft.EntityFrameworkCore;

namespace AuthApi.Application.Common.Interfaces;

public interface IAppDbContext
{
    DbSet<User> Users { get; }
    DbSet<ClientApp> ClientApps { get; }
    DbSet<Role> Roles { get; }
    DbSet<Permission> Permissions { get; }
    DbSet<RolePermission> RolePermissions { get; }
    DbSet<UserAppRole> UserAppRoles { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<ActionToken> ActionTokens { get; }
    DbSet<AuditLog> AuditLogs { get; }
    DbSet<OfficeClassification> OfficeClassifications { get; }
    DbSet<Office> Offices { get; }
    DbSet<OfficeVersion> OfficeVersions { get; }
    DbSet<OrgUnit> OrgUnits { get; }
    DbSet<OrgUnitVersion> OrgUnitVersions { get; }
    DbSet<PositionLevel> PositionLevels { get; }
    DbSet<Position> Positions { get; }
    DbSet<PositionVersion> PositionVersions { get; }
    DbSet<StructureTemplate> StructureTemplates { get; }
    DbSet<TemplateOrgUnit> TemplateOrgUnits { get; }
    DbSet<TemplatePosition> TemplatePositions { get; }
    DbSet<OfficeTemplateApplication> OfficeTemplateApplications { get; }
    DbSet<Employee> Employees { get; }
    DbSet<EmployeeStatus> EmployeeStatuses { get; }
    DbSet<EmployeePosition> EmployeePositions { get; }
    DbSet<EmployeeActingAssignment> EmployeeActingAssignments { get; }
    DbSet<UserEmployeeLink> UserEmployeeLinks { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

