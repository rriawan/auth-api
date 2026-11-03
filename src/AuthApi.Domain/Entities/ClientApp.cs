namespace AuthApi.Domain.Entities;

public class ClientApp
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string ClientId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string ApiKeyHash { get; set; } = string.Empty;
    public string ApiKeyPrefix { get; set; } = string.Empty;
    public string? AllowedOrigins { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }

    public ICollection<Role> Roles { get; set; } = new List<Role>();
    public ICollection<Permission> Permissions { get; set; } = new List<Permission>();
    public ICollection<UserAppRole> UserAppRoles { get; set; } = new List<UserAppRole>();
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
}

