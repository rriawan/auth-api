namespace AuthApi.Application.Common.Models;

public sealed class SecuritySettings
{
    public const string SectionName = "SecuritySettings";

    public int MaxFailedLoginAttempts { get; set; } = 5;
    public int LockoutDurationMinutes { get; set; } = 15;
    public int ActionTokenExpirationMinutes { get; set; } = 30;
    public int PublicEndpointRateLimitPermitCount { get; set; } = 15;
    public int PublicEndpointRateLimitWindowSeconds { get; set; } = 60;
}

