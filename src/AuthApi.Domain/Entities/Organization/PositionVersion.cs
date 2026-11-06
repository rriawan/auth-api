using AuthApi.Domain.Enums.Organization;

namespace AuthApi.Domain.Entities.Organization;

public sealed class PositionVersion : IEffectiveVersion
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PositionId { get; set; }
    public Guid? LevelId { get; set; }
    public Guid? ReportsToPositionId { get; set; }
    public string Name { get; set; } = string.Empty;
    public PositionTrack Track { get; set; }
    public bool IsUnitHead { get; set; }
    public int? MaxIncumbents { get; set; }
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

    public Position Position { get; set; } = null!;
    public PositionLevel? Level { get; set; }
    public Position? ReportsToPosition { get; set; }
}
