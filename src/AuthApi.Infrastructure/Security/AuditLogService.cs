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

            if (detailJson.Length > MaxDetailLength)
            {
                // Keep structured details valid JSON; plain strings are simply cut.
                detailJson = detail is string ? detailJson[..MaxDetailLength] : TruncateJson(detailJson);
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

    private const int MaxDetailLength = 4000;

    /// <summary>Wraps an oversized JSON payload in a small valid document with an escaped preview.</summary>
    internal static string TruncateJson(string json)
    {
        var previewLength = Math.Min(json.Length, MaxDetailLength / 2);
        while (true)
        {
            if (previewLength > 0 && char.IsHighSurrogate(json[previewLength - 1])) previewLength--;
            var wrapped = JsonSerializer.Serialize(new
            {
                truncated = true,
                originalLength = json.Length,
                preview = json[..previewLength]
            });
            if (wrapped.Length <= MaxDetailLength || previewLength == 0) return wrapped;
            previewLength /= 2;
        }
    }

    private static string? Truncate(string? value, int maxLength) =>
        string.IsNullOrEmpty(value) || value.Length <= maxLength
            ? value
            : value[..maxLength];
}

