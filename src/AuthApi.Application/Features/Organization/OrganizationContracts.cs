using System.Text.Json.Serialization;
using AuthApi.Application.Features.Employees;
using AuthApi.Domain.Enums.Organization;

namespace AuthApi.Application.Features.Organization;

// ---- commands ---------------------------------------------------------------------------------------------

public sealed record CreateOfficeCommand(string OfficeCode, string Name, string ClassificationCode, DateOnly EffectiveFrom,
    string Reason, string? DecisionReference = null);
public sealed record OfficeVersionCommand(string Name, string ClassificationCode, bool IsActive, DateOnly EffectiveFrom,
    string Reason, string? DecisionReference = null);

public sealed record CreateUnitCommand(Guid OfficeId, string UnitCode, Guid? ParentOrgUnitId, OrgUnitType UnitType, string Name,
    DateOnly EffectiveFrom, string Reason, string? DecisionReference = null);
public sealed record UnitVersionCommand(Guid? ParentOrgUnitId, OrgUnitType UnitType, string Name, bool IsActive,
    DateOnly EffectiveFrom, string Reason, string? DecisionReference = null);

public sealed record CreatePositionCommand(Guid OrgUnitId, string PositionCode, string Name, PositionTrack Track, bool IsUnitHead,
    int? MaxIncumbents, Guid? LevelId, Guid? ReportsToPositionId, DateOnly EffectiveFrom, string Reason, string? DecisionReference = null);
public sealed record PositionVersionCommand(string Name, PositionTrack Track, bool IsUnitHead, int? MaxIncumbents, Guid? LevelId,
    Guid? ReportsToPositionId, bool IsActive, DateOnly EffectiveFrom, string Reason, string? DecisionReference = null);

/// <param name="Version">Strong ETag of the aggregate after the change.</param>
public sealed record OrganizationMutationResult(Guid Id, string Version, Guid VersionRecordId);

public interface IOrganizationAdminService
{
    Task<OrganizationMutationResult> CreateOfficeAsync(CreateOfficeCommand command, EmployeeActor actor, CancellationToken cancellationToken = default);
    Task<OrganizationMutationResult> AddOfficeVersionAsync(Guid officeId, OfficeVersionCommand command, string? expectedVersion, EmployeeActor actor, CancellationToken cancellationToken = default);
    Task<OrganizationMutationResult> CreateUnitAsync(CreateUnitCommand command, EmployeeActor actor, CancellationToken cancellationToken = default);
    Task<OrganizationMutationResult> AddUnitVersionAsync(Guid unitId, UnitVersionCommand command, string? expectedVersion, EmployeeActor actor, CancellationToken cancellationToken = default);
    Task<OrganizationMutationResult> CreatePositionAsync(CreatePositionCommand command, EmployeeActor actor, CancellationToken cancellationToken = default);
    Task<OrganizationMutationResult> AddPositionVersionAsync(Guid positionId, PositionVersionCommand command, string? expectedVersion, EmployeeActor actor, CancellationToken cancellationToken = default);
}

// ---- read models ------------------------------------------------------------------------------------------

public sealed record ClassificationDto(string Code, string DisplayName);

public sealed record OfficeVersionDto(Guid Id, string ClassificationCode, string Name, DateOnly EffectiveFrom, DateOnly? EffectiveToExclusive,
    bool IsActive, int RevisionNumber, string ChangeReason, string? DecisionReference, DateTime CreatedAtUtc, DateTime? SupersededAtUtc);
public sealed record OfficeSummaryDto(Guid Id, string OfficeCode, string? Name, string? ClassificationCode, bool? IsActive, string Version);
public sealed record OfficeDetailDto(Guid Id, string OfficeCode, string Version, IReadOnlyList<OfficeVersionDto> Versions);
public sealed record OfficePageDto(IReadOnlyList<OfficeSummaryDto> Items, int Page, int PageSize, int TotalCount, DateOnly AsOf);

public sealed record UnitVersionDto(Guid Id, Guid? ParentOrgUnitId, [property: JsonConverter(typeof(JsonStringEnumConverter))] OrgUnitType UnitType,
    string Name, DateOnly EffectiveFrom, DateOnly? EffectiveToExclusive, bool IsActive, int RevisionNumber, string ChangeReason,
    string? DecisionReference, DateTime CreatedAtUtc, DateTime? SupersededAtUtc);
public sealed record UnitSummaryDto(Guid Id, Guid OfficeId, string UnitCode, Guid? ParentOrgUnitId,
    [property: JsonConverter(typeof(JsonStringEnumConverter))] OrgUnitType? UnitType, string? Name, bool? IsActive, string Version);
public sealed record UnitDetailDto(Guid Id, Guid OfficeId, string UnitCode, string Version, IReadOnlyList<UnitVersionDto> Versions);

public sealed record PositionVersionDto(Guid Id, string Name, [property: JsonConverter(typeof(JsonStringEnumConverter))] PositionTrack Track,
    bool IsUnitHead, int? MaxIncumbents, Guid? LevelId, Guid? ReportsToPositionId, DateOnly EffectiveFrom, DateOnly? EffectiveToExclusive,
    bool IsActive, int RevisionNumber, string ChangeReason, string? DecisionReference, DateTime CreatedAtUtc, DateTime? SupersededAtUtc);
public sealed record PositionSummaryDto(Guid Id, Guid OrgUnitId, string PositionCode, string? Name,
    [property: JsonConverter(typeof(JsonStringEnumConverter))] PositionTrack? Track, bool? IsUnitHead, int? MaxIncumbents,
    Guid? ReportsToPositionId, bool? IsActive, string Version, Guid? LevelId = null);
public sealed record PositionDetailDto(Guid Id, Guid OrgUnitId, string PositionCode, string Version, IReadOnlyList<PositionVersionDto> Versions);

public sealed record StructurePositionNode(Guid Id, string PositionCode, string Name,
    [property: JsonConverter(typeof(JsonStringEnumConverter))] PositionTrack Track, bool IsUnitHead, int? MaxIncumbents,
    Guid? ReportsToPositionId, int DefinitiveIncumbents, bool IsVacant);
public sealed record StructureUnitNode(Guid Id, string UnitCode, string Name,
    [property: JsonConverter(typeof(JsonStringEnumConverter))] OrgUnitType UnitType,
    IReadOnlyList<StructurePositionNode> Positions, IReadOnlyList<StructureUnitNode> Children);
public sealed record OfficeStructureDto(Guid OfficeId, string OfficeCode, string? OfficeName, DateOnly AsOf,
    IReadOnlyList<StructureUnitNode> Units);

public interface IOrganizationQueryService
{
    Task<IReadOnlyList<ClassificationDto>> ListClassificationsAsync(EmployeeActor actor, CancellationToken cancellationToken = default);
    Task<OfficePageDto> ListOfficesAsync(string? search, string? classificationCode, DateOnly? asOf, int page, int pageSize,
        EmployeeActor actor, CancellationToken cancellationToken = default);
    Task<OfficeDetailDto> GetOfficeAsync(Guid id, bool includeHistory, EmployeeActor actor, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UnitSummaryDto>> ListUnitsAsync(Guid officeId, DateOnly? asOf, EmployeeActor actor, CancellationToken cancellationToken = default);
    Task<UnitDetailDto> GetUnitAsync(Guid id, bool includeHistory, EmployeeActor actor, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PositionSummaryDto>> ListPositionsAsync(Guid? officeId, Guid? unitId, DateOnly? asOf, EmployeeActor actor, CancellationToken cancellationToken = default);
    Task<PositionDetailDto> GetPositionAsync(Guid id, bool includeHistory, EmployeeActor actor, CancellationToken cancellationToken = default);
    Task<OfficeStructureDto> GetStructureAsync(Guid officeId, DateOnly? asOf, EmployeeActor actor, CancellationToken cancellationToken = default);
}
