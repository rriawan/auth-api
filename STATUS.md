# STATUS.md — Catatan Progres & Memori Kerja `auth-api`

> **Instruksi untuk Model AI:**  
> Baca file ini bersama `AGENTS.md` dan `PLANNING.md` setiap kali memulai sesi baru. Perbarui file ini setiap kali menyelesaikan sebuah fase atau langkah penting agar sesi berikutnya dapat langsung melanjutkan tanpa kehilangan konteks.

**Terakhir Diperbarui:** 2026-11-03

---

## 1. Sedang Dikerjakan

Seluruh tahapan pengembangan **Fase 1 s/d Fase 8** pada `PLANNING.md` telah **selesai 100%**:
- **Status Build:** Lulus bersih (`dotnet build AuthApi.sln` -> `0 Warning(s)`, `0 Error(s)`).
- **Status Pengujian Otomatis:** Lulus 100% (`dotnet test AuthApi.sln` -> `Passed: 8, Failed: 0, Skipped: 0`).

### Ringkasan Progres Fase (`PLANNING.md`)
- [x] **Fase 1 — Inisialisasi Solution & Pondasi .NET 10 LTS** *(Selesai 100%)*
- [x] **Fase 2 — Pemodelan Domain, EF Core 10 & Migrasi MSSQL** *(Selesai 100%)*
- [x] **Fase 3 — Infrastruktur Keamanan, Kriptografi, JWT RS256 & Audit Log** *(Selesai 100%)*
- [x] **Fase 4 — Core Authentication, Refresh Token Rotation & Account Lockout** *(Selesai 100%)*
- [x] **Fase 5 — Alur Aktivasi Akun & Forgot/Reset Password** *(Selesai 100%)*
- [x] **Fase 6 — Autentikasi Client API Key & Endpoint Integrasi Projek Konsumen** *(Selesai 100%)*
- [x] **Fase 7 — Endpoint Administrasi Lengkap (`/api/v1/admin/*`)** *(Selesai 100%)*
- [x] **Fase 8 — Pengujian Otomatis, Hardening & Panduan Integrasi Konsumen** *(Selesai 100%)*
  - Project pengujian `tests/AuthApi.Tests/AuthApi.Tests.csproj` (`net10.0`, xUnit + EF Core InMemory) yang menguji hashing password, hashing token SHA-256, pembuatan/validasi JWT RS256 + JWKS, isolasi hak akses lintas `ClientApp`, *Account Lockout* & *Unlock*, *Refresh Token Rotation* & *Reuse Detection*, aktivasi akun & reset password sekali pakai, serta siklus provisioning admin + validasi realtime `/api/v1/auth/verify`.
  - Middleware `SecurityHeadersMiddleware` (`X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, `Referrer-Policy: no-referrer`, `Cache-Control: no-store` untuk `/api/*`) dan konfigurasi CORS terkontrol (`ConfiguredOrigins`).
  - Dokumen panduan integrasi `INTEGRATION_GUIDE.md` untuk aplikasi konsumen (.NET, Node.js/non-.NET, SPA/Mobile).

---

## 2. Keputusan Penting

1. **Target Framework & SDK:** Seluruh 5 project (`Domain`, `Application`, `Infrastructure`, `WebApi`, dan `Tests`) menggunakan `.NET 10 LTS` (`net10.0`) dengan SDK `10.0.403` yang dikunci melalui `global.json` (`rollForward: latestFeature`).
2. **Isolasi Pengujian dari Database MSSQL (`AGENTS.md` Bagian 7):** Project `AuthApi.Tests` menggunakan `Microsoft.EntityFrameworkCore.InMemory` dengan nama database unik per test instance sehingga eksekusi `dotnet test` tidak pernah menyentuh server MSSQL.
3. **Security Hardening:** Seluruh respons `/api/*` dilengkapi header anti-caching (`Cache-Control: no-store, no-cache, max-age=0`) dan security headers standar melalui `SecurityHeadersMiddleware`.
4. **Git Workflow:** Fase 1–3 telah di-merge ke `main` melalui PR `#1`. Perubahan Fase 4–8 saat ini berada di working tree lokal menunggu instruksi User untuk commit/PR berikutnya.

---

## 3. Masalah yang Diketahui

1. **Tidak Ada Blocker:** Seluruh kode terkompilasi tanpa warning/error dan seluruh pengujian otomatis lulus.

---

## 4. Langkah Berikutnya

1. **Menunggu Instruksi User:**
   - Melakukan commit dan pembuatan Pull Request untuk hasil pekerjaan **Fase 4 s/d Fase 8** apabila diminta oleh User.
   - Atau melakukan pengujian manual melalui Scalar UI (`/scalar/v1`) dan pengembangan fitur tambahan sesuai kebutuhan User.
