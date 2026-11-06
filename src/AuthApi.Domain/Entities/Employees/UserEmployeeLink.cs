namespace AuthApi.Domain.Entities.Employees;

public sealed class UserEmployeeLink
{
    public Guid UserId { get; set; }
    public Guid EmployeeId { get; set; }
    public DateTime LinkedAtUtc { get; set; } = DateTime.UtcNow;
    public Guid? LinkedByUserId { get; set; }
}
