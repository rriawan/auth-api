namespace AuthApi.Domain.Entities.Organization;

/// <summary>Common shape of effective-dated master versions (office, unit, position).</summary>
public interface IEffectiveVersion
{
    DateOnly EffectiveFrom { get; set; }
    DateOnly? EffectiveToExclusive { get; set; }
    int RevisionNumber { get; set; }
    string ChangeReason { get; set; }
    string? DecisionReference { get; set; }
    DateTime CreatedAtUtc { get; set; }
    Guid? CreatedByUserId { get; set; }
    DateTime? SupersededAtUtc { get; set; }
    Guid? SupersededByUserId { get; set; }
    string? SupersededReason { get; set; }
}
