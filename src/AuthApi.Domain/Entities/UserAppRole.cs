namespace AuthApi.Domain.Entities;

public class UserAppRole
{
    public Guid UserId { get; set; }
    public Guid ClientAppId { get; set; }
    public Guid RoleId { get; set; }
    public DateTime AssignedAtUtc { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
    public ClientApp ClientApp { get; set; } = null!;
    public Role Role { get; set; } = null!;
}

