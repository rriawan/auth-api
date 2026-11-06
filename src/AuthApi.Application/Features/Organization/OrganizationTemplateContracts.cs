using System.Text.Json.Serialization;
using AuthApi.Application.Features.Employees;
using AuthApi.Domain.Enums.Organization;

namespace AuthApi.Application.Features.Organization;

public sealed record PublishTemplateCommand(string Reason, string? DecisionReference = null);

/// <param name="IdempotencyKey">Client-chosen key (max 128). Same key + same request replays; same key + different request is a conflict.</param>
public sealed record ApplyTemplateCommand(Guid TemplateId, DateOnly EffectiveFrom, string Reason, string? DecisionReference,
    string IdempotencyKey);

public sealed record TemplateSummaryDto(Guid Id, string ClassificationCode, int Version, string State, int UnitCount, int PositionCount,
    string? Notes, DateTime? PublishedAtUtc, string ETag);

public sealed record TemplatePreviewPositionDto(string PositionCode, string Name, [property: JsonConverter(typeof(JsonStringEnumConverter))] PositionTrack Track,
    bool IsUnitHead, int? MaxIncumbents, string? LevelCode, string? ReportsToCode);
public sealed record TemplatePreviewUnitDto(string UnitCode, string Name, [property: JsonConverter(typeof(JsonStringEnumConverter))] OrgUnitType UnitType,
    string? ParentUnitCode, IReadOnlyList<TemplatePreviewPositionDto> Positions);

public sealed record TemplateApplyPreviewDto(Guid OfficeId, string OfficeCode, string? OfficeName, Guid TemplateId, string TemplateClassificationCode,
    int TemplateVersion, string TemplateState, DateOnly EffectiveFrom, int UnitCount, int PositionCount, bool CanApply,
    IReadOnlyList<string> Blockers, IReadOnlyList<TemplatePreviewUnitDto> Units);

public sealed record TemplateApplyResult(Guid ApplicationId, Guid OfficeId, Guid TemplateId, DateOnly EffectiveFrom, int UnitsCreated,
    int PositionsCreated, bool Replayed);

public sealed record TemplateMutationResult(Guid Id, string Version);
