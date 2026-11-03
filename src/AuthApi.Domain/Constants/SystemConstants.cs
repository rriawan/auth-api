namespace AuthApi.Domain.Constants;

public static class SystemConstants
{
    public const string DefaultAdminClientId = "auth-admin";
    public const string DefaultAdminClientName = "Centralized Auth Administration";
    public const string DefaultSuperAdminRole = "SuperAdmin";
    public const string ApiVersionPrefix = "/api/v1";

    public static class Permissions
    {
        public const string AppsRead = "apps.read";
        public const string AppsWrite = "apps.write";
        public const string AppsRotateKey = "apps.rotate-key";

        public const string UsersRead = "users.read";
        public const string UsersWrite = "users.write";
        public const string UsersUnlock = "users.unlock";
        public const string UsersRevokeSessions = "users.revoke-sessions";

        public const string RolesRead = "roles.read";
        public const string RolesWrite = "roles.write";

        public const string PermissionsRead = "permissions.read";
        public const string PermissionsWrite = "permissions.write";

        public const string AuditLogsRead = "audit-logs.read";

        public static readonly IReadOnlyList<(string Code, string Description)> AllAdminPermissions =
        [
            (AppsRead, "Melihat daftar dan detail aplikasi terdaftar (ClientApps)."),
            (AppsWrite, "Mendaftarkan dan memperbarui aplikasi terdaftar (ClientApps)."),
            (AppsRotateKey, "Merotasi API Key milik aplikasi terdaftar (ClientApps)."),
            (UsersRead, "Melihat daftar dan detail akun pengguna global."),
            (UsersWrite, "Membuat, memperbarui, dan mengatur hak akses pengguna."),
            (UsersUnlock, "Membuka kunci (unlock) akun pengguna yang terkena lockout."),
            (UsersRevokeSessions, "Mencabut seluruh sesi refresh token aktif pengguna."),
            (RolesRead, "Melihat daftar role pada aplikasi."),
            (RolesWrite, "Membuat, mengubah, menghapus role dan memetakan permission ke role."),
            (PermissionsRead, "Melihat katalog permission pada aplikasi."),
            (PermissionsWrite, "Membuat, mengubah, dan menghapus permission pada aplikasi."),
            (AuditLogsRead, "Membaca riwayat keamanan pada Audit Logs.")
        ];
    }
}
