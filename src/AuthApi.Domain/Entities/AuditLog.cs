using AuthApi.Domain.Enums;

namespace AuthApi.Domain.Entities;

public class AuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
    public Guid? UserId { get; set; }
    public Guid? ClientAppId { get; set; }
    public AuditEventType EventType { get; set; }
    public bool IsSuccess { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? DetailJson { get; set; }

    public User? User { get; set; }
    public ClientApp? ClientApp { get; set; }
}

