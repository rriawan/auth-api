namespace AuthApi.Domain.Entities;

public class RefreshToken
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid ClientAppId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public string? CreatedByIp { get; set; }
    public string? UserAgent { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
    public string? ReplacedByTokenHash { get; set; }
    public string? RevokeReason { get; set; }

    public User User { get; set; } = null!;
    public ClientApp ClientApp { get; set; } = null!;

    public bool IsRevoked => RevokedAtUtc.HasValue;
    public bool IsExpired(DateTime utcNow) => utcNow >= ExpiresAtUtc;
    public bool IsActive(DateTime utcNow) => !IsRevoked && !IsExpired(utcNow);
}

