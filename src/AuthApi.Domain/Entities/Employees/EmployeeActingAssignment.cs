using AuthApi.Domain.Entities.Organization;
using AuthApi.Domain.Enums.Employees;

namespace AuthApi.Domain.Entities.Employees;

public sealed class EmployeeActingAssignment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AssignmentKey { get; set; } = Guid.NewGuid();
    public Guid EmployeeId { get; set; }
    public Guid SourceEmployeePositionId { get; set; }
    public Guid TargetPositionId { get; set; }
    public ActingAssignmentType AssignmentType { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveToExclusive { get; set; }
    public EmployeeRecordState RecordState { get; set; } = EmployeeRecordState.Draft;
    public int RevisionNumber { get; set; } = 1;
    public string ChangeReason { get; set; } = string.Empty;
    public string DecisionReference { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public Guid? CreatedByUserId { get; set; }
    public DateTime? SupersededAtUtc { get; set; }
    public Guid? SupersededByUserId { get; set; }
    public string? SupersededReason { get; set; }

    public Employee Employee { get; set; } = null!;
    public EmployeePosition SourceEmployeePosition { get; set; } = null!;
    public Position TargetPosition { get; set; } = null!;
}
