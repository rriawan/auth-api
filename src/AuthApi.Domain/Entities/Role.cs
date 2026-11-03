namespace AuthApi.Domain.Entities;

public class Role
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ClientAppId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsSystemRole { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public ClientApp ClientApp { get; set; } = null!;
    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    public ICollection<UserAppRole> UserAppRoles { get; set; } = new List<UserAppRole>();
}

