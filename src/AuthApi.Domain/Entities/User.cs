namespace AuthApi.Domain.Entities;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public bool IsAccountVerified { get; set; } = false;
    public int FailedLoginCount { get; set; }
    public DateTime? LockoutEndUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }

    public ICollection<UserAppRole> UserAppRoles { get; set; } = new List<UserAppRole>();
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public ICollection<ActionToken> ActionTokens { get; set; } = new List<ActionToken>();
    public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();

    public bool IsLockedOut(DateTime utcNow) =>
        LockoutEndUtc.HasValue && LockoutEndUtc.Value > utcNow;
}

