namespace AuthApi.Domain.Enums;

public enum AuditEventType
{
    SystemBootstrapSeeded,
    LoginSuccess,
    LoginFailed,
    AccountLockedOut,
    TokenRefreshed,
    RefreshTokenReuseDetected,
    Logout,
    AccountActivationRequested,
    AccountActivated,
    PasswordResetRequested,
    PasswordResetCompleted,
    PasswordChanged,
    TokenVerified,
    ClientAppCreated,
    ClientAppUpdated,
    ClientApiKeyRotated,
    UserCreated,
    UserUpdated,
    UserUnlocked,
    UserSessionsRevoked,
    RoleCreated,
    RoleUpdated,
    RoleDeleted,
    RolePermissionsUpdated,
    PermissionCreated,
    PermissionUpdated,
    PermissionDeleted,
    UserAppRolesUpdated
}

