namespace AuthApi.Domain.Entities.Organization;

public sealed class OfficeVersion : IEffectiveVersion
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OfficeId { get; set; }
    public string ClassificationCode { get; set; } = string.Empty;
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

    public Office Office { get; set; } = null!;
    public OfficeClassification Classification { get; set; } = null!;
}
