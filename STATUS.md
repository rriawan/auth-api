# STATUS.md — Catatan Progres & Memori Kerja `auth-api`

> **Instruksi untuk Model AI:**  
> Baca file ini bersama `AGENTS.md`, `PLANNING.md`, dan `PLANNING_FRONTEND.md` setiap kali memulai sesi baru. Perbarui file ini setiap kali menyelesaikan sebuah fase atau langkah penting agar sesi berikutnya dapat langsung melanjutkan tanpa kehilangan konteks.

**Terakhir Diperbarui:** 2026-11-03

---

## 1. Sedang Dikerjakan

- **Backend (`PLANNING.md`):** Seluruh tahapan pengembangan **Fase 1 s/d Fase 8** telah **selesai 100%** dan telah di-merge ke branch `main`.
  - **Status Build:** Lulus bersih (`dotnet build AuthApi.sln` -> `0 Warning(s)`, `0 Error(s)`).
  - **Status Pengujian Otomatis:** Lulus 100% (`dotnet test AuthApi.sln` -> `Passed: 11, Failed: 0, Skipped: 0`).
- **Frontend (`PLANNING_FRONTEND.md`):** **Fase F0**, **Fase F1**, dan **Fase F2** telah **selesai 100%**. Siap melanjutkan ke **Fase F3 (Autentikasi Portal NextAuth v5, Rotasi Token di `proxy.ts`, & Proteksi Route)**.
  - **Status Frontend:** Lulus bersih (`npm run lint` -> `0 problems`, `npm run build` -> `Turbopack compiled 7/7 routes successfully`).

### A. Ringkasan Progres Fase Backend (`PLANNING.md`)
- [x] **Fase 1 — Inisialisasi Solution & Pondasi .NET 10 LTS** *(Selesai 100%)*
- [x] **Fase 2 — Pemodelan Domain, EF Core 10 & Migrasi MSSQL** *(Selesai 100%)*
- [x] **Fase 3 — Infrastruktur Keamanan, Kriptografi, JWT RS256 & Audit Log** *(Selesai 100%)*
- [x] **Fase 4 — Core Authentication, Refresh Token Rotation & Account Lockout** *(Selesai 100%)*
- [x] **Fase 5 — Alur Aktivasi Akun & Forgot/Reset Password** *(Selesai 100%)*
- [x] **Fase 6 — Autentikasi Client API Key & Endpoint Integrasi Projek Konsumen** *(Selesai 100%)*
- [x] **Fase 7 — Endpoint Administrasi Lengkap (`/api/v1/admin/*`)** *(Selesai 100%)*
- [x] **Fase 8 — Pengujian Otomatis, Hardening & Panduan Integrasi Konsumen** *(Selesai 100%)*

### B. Ringkasan Progres Fase Frontend (`PLANNING_FRONTEND.md`)
- [x] **Fase F0 — Penyesuaian Backend untuk Pola BFF (`ForwardedHeaders` & `PortalBaseUrl`)** *(Selesai 100%)*
  - Middleware `ForwardedHeadersOptions` dikonfigurasi dengan `KnownProxies` dan `KnownIPNetworks` (.NET 10) untuk memproses `X-Forwarded-For` dari server Next.js BFF terpercaya, sehingga `RateLimiter` dan `AuditLogs` mencatat IP asli pengguna.
  - Abstraksi notifikasi `LoggingAuthNotificationDispatcher` diperbarui dengan `NotificationSettings:PortalBaseUrl` untuk mencetak tautan lengkap aktivasi akun dan reset password ke portal Next.js.
  - Pengujian otomatis pada `tests/AuthApi.Tests/BffAndForwardedHeadersTests.cs` memvalidasi link notifikasi, resolusi IP dari proxy tepercaya ke audit log, dan penolakan spoofing IP dari sumber tidak tepercaya.
- [x] **Fase F1 — Inisialisasi Proyek `web/auth-admin-portal` dari Template** *(Selesai 100%)*
  - Menyalin template `rriawan/TEMPLATE_NEXT` ke `web/auth-admin-portal` tanpa `.git`, `node_modules`, `.next`.
  - Membersihkan 22 folder demo dashboard yang tidak relevan, provider Google/GitHub, halaman register publik, mock database `utils/db.ts`, serta demo components.
  - Memperbarui branding ke "Auth Admin Portal", menata ulang `sidebar-data.ts` (Dashboard, Manajemen Akses, Keamanan, Akun Saya), merapikan Header & Footer, serta menyetel `next.config.ts` dengan Security Headers.
  - Menyiapkan `.env.example` dan `.env.local` untuk integrasi backend `http://localhost:5100` (`clientId = auth-admin`).
  - Memperbarui `.gitignore` di root dan folder web.
  - `npm run lint` dan `npm run build` lulus 100% tanpa error/warning.
- [x] **Fase F2 — Lapisan API Client (`lib/api`) & Tipe Kontrak** *(Selesai 100%)*
  - Membuat `lib/api/types.ts`: Pemetaan tipe DTO TypeScript 1:1 dari backend (`AuthDtos.cs`, `AdminDtos.cs`, `JwksModels.cs`), `ProblemDetails` (RFC 9457), dan `PagedResponse<T>`.
  - Membuat `lib/api/errors.ts`: Kelas `ApiException` yang memetakan error ProblemDetails RFC 9457 lengkap dengan helper status HTTP (`isUnauthorized`, `isForbidden`, `isNotFound`, `isConflict`, `isRateLimited`, `isServerError`).
  - Membuat `lib/api/client.ts` (`server-only`): Fungsi `apiFetch<T>` terpusat dengan penanganan timeout, header forwarding (`X-Forwarded-For`, `User-Agent`) otomatis dari incoming headers Next.js, `cache: 'no-store'`, dan parser ProblemDetails.
  - Membuat domain API modules: `authApi`, `appsApi`, `usersApi`, `rolesApi`, `permissionsApi`, `userAppRolesApi`, dan `auditLogsApi` di `lib/api/`.
  - Membuat skema validasi Zod lengkap di `lib/validation/` (`auth.schema.ts`, `apps.schema.ts`, `users.schema.ts`, `roles.schema.ts`, `permissions.schema.ts`) dengan aturan bisnis selaras backend (panjang password 8-256 karakter, format `clientId`, pola permission `modul.aksi`).
  - Mengganti import Google Fonts pada `app/layout.tsx` dengan font-sans native untuk stabilitas proses build offline/sandbox.
  - Verifikasi: `npm run lint` (0 problems), `npm run build` (Turbopack 7/7 routes lulus bersih), dan `dotnet test` (11/11 tests lulus).
- [ ] **Fase F3 — Autentikasi Portal (NextAuth v5 + Rotasi Token di `proxy.ts`) & Proteksi Route**
- [ ] **Fase F4 — Halaman Self-Service Publik (Forgot/Reset Password & Aktivasi Akun)**
- [ ] **Fase F5 — Profil & Ganti Password**
- [ ] **Fase F6 — Manajemen Aplikasi (`ClientApps`) & API Key**
- [ ] **Fase F7 — Permissions & Roles per Aplikasi**
- [ ] **Fase F8 — Manajemen User & Penugasan Akses**
- [ ] **Fase F9 — Audit Logs & Dashboard**
- [ ] **Fase F10 — Hardening, Pengujian & Dokumentasi**

---

## 2. Keputusan Penting

1. **Target Framework & SDK Backend:** Seluruh 5 project (`Domain`, `Application`, `Infrastructure`, `WebApi`, dan `Tests`) menggunakan `.NET 10 LTS` (`net10.0`) dengan SDK `10.0.403` yang dikunci melalui `global.json` (`rollForward: latestFeature`).
2. **Isolasi Pengujian dari Database MSSQL (`AGENTS.md` Bagian 7):** Project `AuthApi.Tests` menggunakan `Microsoft.EntityFrameworkCore.InMemory` dengan nama database unik per test instance sehingga eksekusi `dotnet test` tidak pernah menyentuh server MSSQL.
3. **Arsitektur Frontend (`PLANNING_FRONTEND.md`):**
   - Ditempatkan secara monorepo pada folder `web/auth-admin-portal` menggunakan **Next.js 16 (App Router) + React 19 + Tailwind CSS v4 + shadcn/ui** mengacu pada `rriawan/TEMPLATE_NEXT`.
   - Menggunakan pola **BFF (Backend-for-Frontend)** dengan **NextAuth v5 Credentials Provider** (`clientId = auth-admin`).
   - Ukuran cookie session dijaga di bawah `4096 bytes` dengan tidak menduplikasi array `roles`/`permissions` di dalam cookie (diekstrak dari `accessToken` di sisi server).
   - Pemeriksaan kedaluwarsa & rotasi `refreshToken` wajib dilakukan di `proxy.ts` (atau Route Handler / Server Action) agar cookie baru selalu tertulis kembali ke browser (`Set-Cookie`) dan tidak memicu *Refresh Token Reuse Detection* saat render Server Component.
   - Penambahan fitur daftar user per aplikasi untuk aplikasi konsumen dan aksi bantuan reset password oleh admin dipisahkan ke perencanaan tersendiri.
4. **Forwarded Headers & Reverse Proxy Trust (Fase F0):**
   - `UseForwardedHeaders` dijalankan sebelum `UseRateLimiter` dan controller agar seluruh inspeksi IP (rate limiting publik dan audit log) melihat IP asli dari proxy tepercaya (`127.0.0.1`, `::1` atau konfigurasi di `appsettings.json`).
   - Sumber yang tidak terdaftar di `KnownProxies`/`KnownIPNetworks` tidak dapat melakukan spoofing header `X-Forwarded-For`.
5. **Pembersihan Template & Next.js 16 Compatibility (Fase F1):**
   - Menggunakan `.npmrc` dengan `legacy-peer-deps=true` dan `package.json` overrides untuk menjembatani peer dependency `next-auth@5.0.0-beta.29` pada Next.js 16.
   - Menghapus komponen demo yang tidak relevan (crypto, nft, charts demo, calendar, chat, email, marketplace, theme customizer) agar project ramping, cepat di-build, dan mematuhi aturan strict compiler React 19.

---

## 3. Masalah yang Diketahui

1. **Tidak Ada Blocker:** Baik backend (.NET 10) maupun frontend (Next.js 16) lulus build dan lint 100%.

---

## 4. Langkah Berikutnya

1. **Menunggu Instruksi User:**
   - Melakukan commit untuk hasil pekerjaan **Fase F2 (Lapisan API Client & Skema Validasi Zod)**, atau
   - Langsung melanjutkan ke **Fase F3 (Autentikasi Portal NextAuth v5, Rotasi Token di `proxy.ts`, & Proteksi Route)**.
