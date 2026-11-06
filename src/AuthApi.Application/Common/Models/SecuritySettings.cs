namespace AuthApi.Application.Common.Models;

public sealed class SecuritySettings
{
    public const string SectionName = "SecuritySettings";

    public int MaxFailedLoginAttempts { get; set; } = 5;
    public int LockoutDurationMinutes { get; set; } = 15;
    public int ActionTokenExpirationMinutes { get; set; } = 30;
    public int PublicEndpointRateLimitPermitCount { get; set; } = 15;
    public int PublicEndpointRateLimitWindowSeconds { get; set; } = 60;

    /// <summary>
    /// When true, login, refresh, realtime verification and permission lookup refuse accounts linked to an employee whose
    /// effective status is not ACTIVE (D-02). Off by default: enable only after link/status data has been validated.
    /// </summary>
    public bool EnforceEmploymentStatus { get; set; }
}

