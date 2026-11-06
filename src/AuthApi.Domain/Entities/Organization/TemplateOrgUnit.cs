using AuthApi.Domain.Enums.Organization;

namespace AuthApi.Domain.Entities.Organization;

public sealed class TemplateOrgUnit
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TemplateId { get; set; }
    public Guid? ParentTemplateOrgUnitId { get; set; }
    public string UnitCode { get; set; } = string.Empty;
    public OrgUnitType UnitType { get; set; }
    public string Name { get; set; } = string.Empty;

    public StructureTemplate Template { get; set; } = null!;
    public TemplateOrgUnit? Parent { get; set; }
    public ICollection<TemplateOrgUnit> Children { get; set; } = [];
    public ICollection<TemplatePosition> Positions { get; set; } = [];
}
