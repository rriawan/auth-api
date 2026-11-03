using System.Text.Json;
using AuthApi.Application.Common.Interfaces;
using AuthApi.Domain.Entities;
using AuthApi.Domain.Enums;
using AuthApi.Infrastructure.Persistence;

namespace AuthApi.Infrastructure.Security;

public sealed class AuditLogService(AppDbContext dbContext) : IAuditLogService
{
    public async Task LogAsync(
        AuditEventType eventType,
        bool isSuccess,
        Guid? userId = null,
        Guid? clientAppId = null,
        string? ipAddress = null,
        string? userAgent = null,
        object? detail = null,
        CancellationToken cancellationToken = default)
    {
        string? detailJson = null;
        if (detail is not null)
        {
            detailJson = detail is string rawString
                ? rawString
                : JsonSerializer.Serialize(detail);

            if (detailJson.Length > 4000)
            {
                detailJson = detailJson[..4000];
            }
        }

        var auditLog = new AuditLog
        {
            Id = Guid.NewGuid(),
            TimestampUtc = DateTime.UtcNow,
            UserId = userId,
            ClientAppId = clientAppId,
            EventType = eventType,
            IsSuccess = isSuccess,
            IpAddress = Truncate(ipAddress, 64),
            UserAgent = Truncate(userAgent, 512),
            DetailJson = detailJson
        };

        dbContext.AuditLogs.Add(auditLog);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static string? Truncate(string? value, int maxLength) =>
        string.IsNullOrEmpty(value) || value.Length <= maxLength
            ? value
            : value[..maxLength];
}

