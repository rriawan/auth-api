using AuthApi.Domain.Enums.Organization;

namespace AuthApi.Domain.Entities.Organization;

public sealed class OrgUnitVersion : IEffectiveVersion
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrgUnitId { get; set; }
    public Guid? ParentOrgUnitId { get; set; }
    public OrgUnitType UnitType { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveToExclusive { get; set; }
    public bool IsActive { get; set; } = true;
    public int RevisionNumber { get; set; } = 1;
    public string ChangeReason { get; set; } = string.Empty;
    public string? DecisionReference { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public Guid? CreatedByUserId { get; set; }
    public DateTime? SupersededAtUtc { get; set; }
    public Guid? SupersededByUserId { get; set; }
    public string? SupersededReason { get; set; }

    public OrgUnit OrgUnit { get; set; } = null!;
    public OrgUnit? ParentOrgUnit { get; set; }
}
