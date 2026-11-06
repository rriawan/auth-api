using AuthApi.Domain.Enums.Organization;

namespace AuthApi.Domain.Entities.Organization;

public sealed class TemplatePosition
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TemplateId { get; set; }
    public Guid TemplateOrgUnitId { get; set; }
    public Guid? ReportsToTemplatePositionId { get; set; }
    public Guid? LevelId { get; set; }
    public string PositionCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public PositionTrack Track { get; set; }
    public bool IsUnitHead { get; set; }
    public int? MaxIncumbents { get; set; }

    public StructureTemplate Template { get; set; } = null!;
    public TemplateOrgUnit OrgUnit { get; set; } = null!;
    public TemplatePosition? ReportsToPosition { get; set; }
}
