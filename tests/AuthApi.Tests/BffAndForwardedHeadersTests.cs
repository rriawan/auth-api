using System.Net;
using AuthApi.Application.Common.Models;
using AuthApi.Domain.Enums;
using AuthApi.Infrastructure.Notifications;
using AuthApi.Infrastructure.Persistence;
using AuthApi.Infrastructure.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace AuthApi.Tests;

public class BffAndForwardedHeadersTests
{
    [Fact]
    public async Task NotificationDispatcher_GeneratesCorrectPortalUrls()
    {
        var testLogger = new TestLogger<LoggingAuthNotificationDispatcher>();
        var settings = Options.Create(new NotificationSettings
        {
            PortalBaseUrl = "https://admin.portal.local/"
        });

        var dispatcher = new LoggingAuthNotificationDispatcher(testLogger, settings);

        // 1. Activation token test
        await dispatcher.SendAccountActivationTokenAsync(
            Guid.NewGuid(),
            "user@test.local",
            "Test User",
            "act-token-xyz-123",
            DateTime.UtcNow.AddHours(24));

        Assert.Contains(testLogger.LoggedMessages, m =>
            m.Contains("https://admin.portal.local/auth/activate-account/confirm?token=act-token-xyz-123"));

        // 2. Reset token test
        await dispatcher.SendPasswordResetTokenAsync(
            Guid.NewGuid(),
            "user@test.local",
            "Test User",
            "reset-token-abc-789",
            DateTime.UtcNow.AddMinutes(30));

        Assert.Contains(testLogger.LoggedMessages, m =>
            m.Contains("https://admin.portal.local/auth/reset-password?token=reset-token-abc-789"));
    }

    [Fact]
    public async Task ForwardedHeaders_FromTrustedProxy_ResolvesOriginalClientIp_AndRecordsInAuditLog()
    {
        // Setup in-memory db & audit log service
        var dbOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"ForwardedHeadersTestDb_{Guid.NewGuid():N}")
            .Options;
        await using var dbContext = new AppDbContext(dbOptions);
        var auditLogService = new AuditLogService(dbContext);

        // Setup ForwardedHeadersMiddleware with 127.0.0.1 as trusted proxy
        var options = Options.Create(new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
            ForwardLimit = 1
        });
        options.Value.KnownProxies.Clear();
        options.Value.KnownProxies.Add(IPAddress.Parse("127.0.0.1"));
        options.Value.KnownIPNetworks.Clear();

        string? resolvedIp = null;
        var middleware = new ForwardedHeadersMiddleware(
            next: ctx =>
            {
                resolvedIp = ctx.Connection.RemoteIpAddress?.ToString();
                return Task.CompletedTask;
            },
            loggerFactory: NullLoggerFactory.Instance,
            options: options);

        // Simulate request from trusted proxy (127.0.0.1) forwarding real client IP (203.0.113.195)
        var httpContext = new DefaultHttpContext();
        httpContext.Connection.RemoteIpAddress = IPAddress.Parse("127.0.0.1");
        httpContext.Request.Headers["X-Forwarded-For"] = "203.0.113.195";
        httpContext.Request.Headers["User-Agent"] = "Mozilla/5.0 (Windows NT 10.0; Win64; x64)";

        await middleware.Invoke(httpContext);

        // Assert RemoteIpAddress was resolved to 203.0.113.195
        Assert.Equal("203.0.113.195", resolvedIp);
        Assert.Equal("203.0.113.195", httpContext.Connection.RemoteIpAddress?.ToString());

        // Now test AuditLog recording with the resolved IP
        await auditLogService.LogAsync(
            eventType: AuditEventType.LoginSuccess,
            isSuccess: true,
            userId: Guid.NewGuid(),
            clientAppId: null,
            ipAddress: httpContext.Connection.RemoteIpAddress?.ToString(),
            userAgent: httpContext.Request.Headers.UserAgent.ToString(),
            detail: "Success from BFF");

        var loggedEntry = await dbContext.AuditLogs.SingleAsync();
        Assert.Equal("203.0.113.195", loggedEntry.IpAddress);
        Assert.Equal("Mozilla/5.0 (Windows NT 10.0; Win64; x64)", loggedEntry.UserAgent);
    }

    [Fact]
    public async Task ForwardedHeaders_FromUntrustedSource_IgnoresXForwardedFor_AndPreservesDirectIp()
    {
        // Setup in-memory db & audit log service
        var dbOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"ForwardedHeadersUntrustedDb_{Guid.NewGuid():N}")
            .Options;
        await using var dbContext = new AppDbContext(dbOptions);
        var auditLogService = new AuditLogService(dbContext);

        // Setup ForwardedHeadersMiddleware trusting ONLY 127.0.0.1
        var options = Options.Create(new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
            ForwardLimit = 1
        });
        options.Value.KnownProxies.Clear();
        options.Value.KnownProxies.Add(IPAddress.Parse("127.0.0.1"));
        options.Value.KnownIPNetworks.Clear();

        string? resolvedIp = null;
        var middleware = new ForwardedHeadersMiddleware(
            next: ctx =>
            {
                resolvedIp = ctx.Connection.RemoteIpAddress?.ToString();
                return Task.CompletedTask;
            },
            loggerFactory: NullLoggerFactory.Instance,
            options: options);

        // Simulate direct connection from untrusted IP (198.51.100.5) attempting to spoof X-Forwarded-For (203.0.113.195)
        var httpContext = new DefaultHttpContext();
        httpContext.Connection.RemoteIpAddress = IPAddress.Parse("198.51.100.5");
        httpContext.Request.Headers["X-Forwarded-For"] = "203.0.113.195";

        await middleware.Invoke(httpContext);

        // Assert RemoteIpAddress was NOT changed to the spoofed header value
        Assert.Equal("198.51.100.5", resolvedIp);
        Assert.Equal("198.51.100.5", httpContext.Connection.RemoteIpAddress?.ToString());

        // AuditLog must record the actual untrusted source IP, not the spoofed IP
        await auditLogService.LogAsync(
            eventType: AuditEventType.LoginFailed,
            isSuccess: false,
            userId: null,
            clientAppId: null,
            ipAddress: httpContext.Connection.RemoteIpAddress?.ToString(),
            userAgent: "curl/8.0",
            detail: "Failed login attempt");

        var loggedEntry = await dbContext.AuditLogs.SingleAsync();
        Assert.Equal("198.51.100.5", loggedEntry.IpAddress);
    }

    private sealed class TestLogger<T> : ILogger<T>
    {
        public List<string> LoggedMessages { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            LoggedMessages.Add(formatter(state, exception));
        }
    }
}
