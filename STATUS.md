# STATUS.md — Catatan Progres & Memori Kerja `auth-api`

> **Instruksi untuk Model AI:**  
> Baca file ini bersama `AGENTS.md` dan `PLANNING.md` setiap kali memulai sesi baru. Perbarui file ini setiap kali menyelesaikan sebuah fase atau langkah penting agar sesi berikutnya dapat langsung melanjutkan tanpa kehilangan konteks.

**Terakhir Diperbarui:** 2026-11-03

---

## 1. Sedang Dikerjakan

Saat ini pengerjaan telah **menyelesaikan Fase 3** dan siap masuk ke **Fase 4 — Core Authentication, Refresh Token Rotation & Account Lockout**:
- **Status Fase 2 (Database & Seeder):** Selesai 100% (migrasi `InitialCreate` dan proses `--seed` telah dieksekusi oleh User, `RawApiKey` `auth-admin` dan password awal `superadmin` telah disimpan oleh User).
- **Status Fase 3 (Kriptografi, JWT RS256, JWKS & Audit Log):** Selesai 100% dan lulus `dotnet build AuthApi.sln` (`0 Warning(s)`, `0 Error(s)`).

### Ringkasan Progres Fase (`PLANNING.md`)
- [x] **Fase 1 — Inisialisasi Solution & Pondasi .NET 10 LTS** *(Selesai)*
  - `global.json` (SDK `10.0.403`), `.gitignore`, `AuthApi.sln`, dan 4 project (`Domain`, `Application`, `Infrastructure`, `WebApi`) dengan `<TargetFramework>net10.0</TargetFramework>`.
  - `GlobalExceptionHandler` (RFC 9457 `ProblemDetails`), `/health`, OpenAPI `.NET 10` + Scalar UI (`/scalar/v1`).
- [x] **Fase 2 — Pemodelan Domain, EF Core 10 & Migrasi MSSQL** *(Selesai 100%)*
  - 9 entitas inti di `AuthApi.Domain`, `IAppDbContext` di `AuthApi.Application`, `AppDbContext` + 9 `IEntityTypeConfiguration<T>` + migration `20261103030011_InitialCreate` + `DatabaseSeeder` di `AuthApi.Infrastructure`.
  - Migrasi dan seeding awal di MSSQL telah dijalankan oleh User.
- [x] **Fase 3 — Infrastruktur Keamanan, Kriptografi, JWT RS256 & Audit Log** *(Selesai 100%)*
  - Layanan hashing password (`IPasswordHasherService` / `PasswordHasherService` berbasis `PasswordHasher<User>` PBKDF2-SHA512).
  - Layanan kriptografi token & API key (`ITokenCryptoService` / `TokenCryptoService` berbasis `RandomNumberGenerator`, SHA-256 hex, dan `CryptographicOperations.FixedTimeEquals`).
  - Manajemen kunci RSA 2048-bit (`RsaKeyProvider`) dan layanan JWT RS256 (`IJwtTokenService` / `JwtTokenService`) dengan klaim terisolasi per `ClientApp` (`sub`, `unique_name`, `email`, `aud`, `roles`, `permissions`, `jti`, `iat`, `exp`).
  - Endpoint publik `GET /.well-known/jwks.json` (`DiscoveryController`) berstandar RFC 7517 JWKS.
  - Konfigurasi middleware JWT Bearer (`JwtBearerOptionsSetup`) dan layanan pencatatan audit (`IAuditLogService` / `AuditLogService`).
- [ ] **Fase 4 — Core Authentication, Refresh Token Rotation & Account Lockout** *(Siap Dikerjakan)*
- [ ] **Fase 5 — Alur Aktivasi Akun & Forgot/Reset Password** *(Belum Dimulai)*
- [ ] **Fase 6 — Autentikasi Client API Key & Endpoint Integrasi Projek Konsumen** *(Belum Dimulai)*
- [ ] **Fase 7 — Endpoint Administrasi Lengkap (`/api/v1/admin/*`)** *(Belum Dimulai)*
- [ ] **Fase 8 — Pengujian Otomatis, Hardening & Panduan Integrasi Konsumen** *(Belum Dimulai)*

---

## 2. Keputusan Penting

1. **Target Framework & SDK:** Seluruh project menggunakan `.NET 10 LTS` (`net10.0`) dengan SDK `10.0.403` yang dikunci melalui `global.json` (`rollForward: latestFeature`).
2. **Abstraksi Database (`IAppDbContext`):** Layer `AuthApi.Application` mengakses database melalui interface `IAppDbContext`, sedangkan implementasi `AppDbContext` berada di `AuthApi.Infrastructure`.
3. **Pencegahan Multiple Cascade Paths di MSSQL:**
   - `RolePermission`: FK ke `Role` (`Cascade`), FK ke `Permission` (`Restrict`).
   - `UserAppRole`: FK ke `User` & `Role` (`Cascade`), FK ke `ClientApp` (`Restrict`).
   - `AuditLog`: FK ke `User` & `ClientApp` (`SetNull`) untuk menjaga sifat *append-only*.
4. **Manajemen Kunci RSA (RS256 & JWKS):**
   - `RsaKeyProvider` membaca `JwtSettings:RsaPrivateKeyPem` dari konfigurasi/User Secrets/Environment Variable.
   - Jika belum diset pada environment Development, otomatis membuat dan memuat file kunci RSA 2048-bit lokal `dev-rsa-private.key` di folder aplikasi (sudah di-ignore oleh `.gitignore` melalui aturan `*.key`) agar token tetap konsisten lintas restart selama development.
   - `CryptoProviderFactory.CacheSignatureProviders = false` diterapkan pada `RsaSecurityKey` penandatangan agar instance `RSA` singleton tidak ter-dispose oleh `JwtSecurityTokenHandler`.
5. **Klaim JWT Tanpa Pemetaan Klaim Legacy:**
   - `MapInboundClaims = false` diaktifkan pada `JwtBearerOptions` dan `JwtSecurityTokenHandler` agar klaim standar (`sub`, `unique_name`, `email`, `aud`, `roles`, `permissions`, `jti`) tetap konsisten dan tidak berubah menjadi URI XML SOAP lama.

---

## 3. Masalah yang Diketahui

1. **Tidak Ada Blocker Saat Ini:** Seluruh project terkompilasi bersih (`0 Warning(s)`, `0 Error(s)`), database MSSQL sudah terhubung dan ter-seed.

---

## 4. Langkah Berikutnya

1. **Kerjakan Fase 4 — Core Authentication, Refresh Token Rotation & Account Lockout:**
   - Buat DTO dan service autentikasi (`IAuthService` / `AuthService`) di `AuthApi.Application` untuk:
     - `POST /api/v1/auth/login` (validasi kredensial `username/email` + `password` + `clientId`, cek `IsActive`, `IsAccountVerified`, `LockoutEndUtc`, hitung `FailedLoginCount`, terbitkan Access Token JWT RS256 + Refresh Token yang di-hash SHA-256, catat `AuditLog`).
     - `POST /api/v1/auth/refresh` (validasi & rotasi refresh token, deteksi *Refresh Token Reuse* yang otomatis mencabut seluruh sesi aktif user tersebut, catat `AuditLog`).
     - `POST /api/v1/auth/logout` (cabut refresh token yang sedang digunakan dan catat `AuditLog`).
     - `GET /api/v1/auth/me` (ambil profil user yang sedang login beserta daftar `roles` & `permissions` pada `clientId` dari token/database).
     - `POST /api/v1/auth/change-password` (verifikasi `currentPassword`, hash `newPassword`, cabut sesi refresh token aktif lain, catat `AuditLog`).
   - Buat `AuthController` di `AuthApi.WebApi` dan aktifkan **Built-in ASP.NET Core Rate Limiting** berbasis IP pada endpoint publik sensitif.
   - Verifikasi dengan `dotnet build AuthApi.sln` dan perbarui `STATUS.md`.
