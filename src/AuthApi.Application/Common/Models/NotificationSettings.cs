namespace AuthApi.Application.Common.Models;

public sealed class NotificationSettings
{
    public const string SectionName = "Notification";

    /// <summary>
    /// Base URL Auth Admin Portal atau web client (contoh: http://localhost:3000 atau https://auth.company.com).
    /// Digunakan untuk menyusun tautan reset password dan aktivasi akun pada notifikasi.
    /// </summary>
    public string PortalBaseUrl { get; set; } = "http://localhost:3000";
}

