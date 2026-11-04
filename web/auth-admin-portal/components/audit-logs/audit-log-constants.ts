export interface AuditEventMeta {
  value: string;
  label: string;
  category: "Autentikasi" | "Akun Mandiri" | "Aplikasi" | "Pengguna" | "Akses & Role" | "Sistem";
}

export const AUDIT_EVENT_TYPES: AuditEventMeta[] = [
  { value: "LoginSuccess", label: "Login Berhasil", category: "Autentikasi" },
  { value: "LoginFailed", label: "Login Gagal", category: "Autentikasi" },
  { value: "AccountLockedOut", label: "Akun Terkunci (Lockout)", category: "Autentikasi" },
  { value: "TokenRefreshed", label: "Rotasi Refresh Token", category: "Autentikasi" },
  { value: "RefreshTokenReuseDetected", label: "Deteksi Reuse Refresh Token", category: "Autentikasi" },
  { value: "Logout", label: "Logout Sesi", category: "Autentikasi" },
  { value: "TokenVerified", label: "Verifikasi Token Realtime", category: "Autentikasi" },
  { value: "AccountActivationRequested", label: "Permintaan Aktivasi Akun", category: "Akun Mandiri" },
  { value: "AccountActivated", label: "Akun Berhasil Diaktifkan", category: "Akun Mandiri" },
  { value: "PasswordResetRequested", label: "Permintaan Reset Password", category: "Akun Mandiri" },
  { value: "PasswordResetCompleted", label: "Reset Password Selesai", category: "Akun Mandiri" },
  { value: "PasswordChanged", label: "Ganti Password Mandiri", category: "Akun Mandiri" },
  { value: "ClientAppCreated", label: "Aplikasi Klien Didaftarkan", category: "Aplikasi" },
  { value: "ClientAppUpdated", label: "Aplikasi Klien Diperbarui", category: "Aplikasi" },
  { value: "ClientApiKeyRotated", label: "Rotasi API Key Aplikasi", category: "Aplikasi" },
  { value: "UserCreated", label: "Pengguna Baru Dibuat", category: "Pengguna" },
  { value: "UserUpdated", label: "Profil Pengguna Diperbarui", category: "Pengguna" },
  { value: "UserUnlocked", label: "Kunci Akun Pengguna Dibuka", category: "Pengguna" },
  { value: "UserSessionsRevoked", label: "Seluruh Sesi Pengguna Dicabut", category: "Pengguna" },
  { value: "RoleCreated", label: "Role Baru Dibuat", category: "Akses & Role" },
  { value: "RoleUpdated", label: "Role Diperbarui", category: "Akses & Role" },
  { value: "RoleDeleted", label: "Role Dihapus", category: "Akses & Role" },
  { value: "RolePermissionsUpdated", label: "Pemetaan Izin Role Diperbarui", category: "Akses & Role" },
  { value: "PermissionCreated", label: "Permission Baru Dibuat", category: "Akses & Role" },
  { value: "PermissionUpdated", label: "Permission Diperbarui", category: "Akses & Role" },
  { value: "PermissionDeleted", label: "Permission Dihapus", category: "Akses & Role" },
  { value: "UserAppRolesUpdated", label: "Penugasan Role Pengguna Diperbarui", category: "Akses & Role" },
  { value: "SystemBootstrapSeeded", label: "Inisialisasi Bootstrap Sistem", category: "Sistem" },
];

export function getAuditEventLabel(eventType: string): string {
  const found = AUDIT_EVENT_TYPES.find(
    (item) => item.value.toLowerCase() === eventType.toLowerCase()
  );
  return found ? found.label : eventType;
}

export function prettyPrintDetailJson(detailJson: string | null): string {
  if (!detailJson) return "{\n  \"info\": \"Tidak ada payload detail tambahan.\"\n}";
  try {
    const parsed: unknown = JSON.parse(detailJson);
    return JSON.stringify(parsed, null, 2);
  } catch {
    return detailJson;
  }
}

