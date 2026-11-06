namespace AuthApi.Domain.Entities.Employees;

public sealed class Employee
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string EmployeeNumber { get; set; } = string.Empty;
    public string EmployeeFullName { get; set; } = string.Empty;
    public byte[] RowVersion { get; set; } = [];
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public Guid? CreatedByUserId { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public Guid? UpdatedByUserId { get; set; }

    public ICollection<EmployeeStatus> Statuses { get; set; } = [];
    public ICollection<EmployeePosition> Positions { get; set; } = [];
    public ICollection<EmployeeActingAssignment> ActingAssignments { get; set; } = [];
}
