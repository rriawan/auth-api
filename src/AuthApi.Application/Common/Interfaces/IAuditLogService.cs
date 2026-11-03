using AuthApi.Domain.Enums;

namespace AuthApi.Application.Common.Interfaces;

public interface IAuditLogService
{
    Task LogAsync(
        AuditEventType eventType,
        bool isSuccess,
        Guid? userId = null,
        Guid? clientAppId = null,
        string? ipAddress = null,
        string? userAgent = null,
        object? detail = null,
        CancellationToken cancellationToken = default);
}

