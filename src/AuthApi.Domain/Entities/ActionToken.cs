using AuthApi.Domain.Enums;

namespace AuthApi.Domain.Entities;

public class ActionToken
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public ActionTokenPurpose Purpose { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? UsedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;

    public bool IsUsed => UsedAtUtc.HasValue;
    public bool IsExpired(DateTime utcNow) => utcNow >= ExpiresAtUtc;
    public bool IsValid(DateTime utcNow) => !IsUsed && !IsExpired(utcNow);
}

