using AuthApi.Domain.Entities.Organization;
using AuthApi.Domain.Enums.Employees;

namespace AuthApi.Domain.Entities.Employees;

public sealed class EmployeePosition
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AssignmentKey { get; set; } = Guid.NewGuid();
    public Guid EmployeeId { get; set; }
    public Guid PositionId { get; set; }
    public bool IsPrimary { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveToExclusive { get; set; }
    public EmployeeRecordState RecordState { get; set; } = EmployeeRecordState.Draft;
    public int RevisionNumber { get; set; } = 1;
    public string ChangeReason { get; set; } = string.Empty;
    public string? DecisionReference { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public Guid? CreatedByUserId { get; set; }
    public DateTime? SupersededAtUtc { get; set; }
    public Guid? SupersededByUserId { get; set; }
    public string? SupersededReason { get; set; }

    public Employee Employee { get; set; } = null!;
    public Position Position { get; set; } = null!;
    public ICollection<EmployeeActingAssignment> ActingAssignments { get; set; } = [];
}
