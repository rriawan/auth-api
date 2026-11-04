# STATUS.md — Catatan Progres & Memori Kerja `auth-api`

> **Instruksi untuk Model AI:**  
> Baca file ini bersama `AGENTS.md`, `PLANNING.md`, dan `PLANNING_FRONTEND.md` setiap kali memulai sesi baru. Perbarui file ini setiap kali menyelesaikan sebuah fase atau langkah penting agar sesi berikutnya dapat langsung melanjutkan tanpa kehilangan konteks.

**Terakhir Diperbarui:** 2026-11-04

---

## 1. Sedang Dikerjakan

- **Backend (`PLANNING.md`):** Seluruh tahapan pengembangan **Fase 1 s/d Fase 8** telah **selesai 100%** dan telah di-merge ke branch `main`.
  - **Status Build:** Lulus bersih (`dotnet build AuthApi.sln` -> `0 Warning(s)`, `0 Error(s)`).
  - **Status Pengujian Otomatis:** Lulus 100% (`dotnet test AuthApi.sln` -> `Passed: 11, Failed: 0, Skipped: 0`).
- **Frontend (`PLANNING_FRONTEND.md`):** Seluruh tahapan pengembangan **Fase F0 s/d Fase F10** telah **selesai 100%**. Seluruh roadmap frontend selesai dikerjakan!
  - **Status Linting:** Lulus bersih (`npm run lint` -> `0 problems`, `0 errors`, `0 warnings`).
  - **Status Pengujian Unit:** Lulus 100% (`npm run test` -> `Passed: 24, Failed: 0, 0 cancelled, 0 skipped`).
  - **Status Build Produksi:** Lulus bersih (`npm run build` -> `Turbopack compiled 15/15 routes successfully`).

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
- [x] **Fase F3 — Autentikasi Portal (NextAuth v5 + Rotasi Token di `proxy.ts`) & Proteksi Route** *(Selesai 100%)*
  - Mengonfigurasi `auth.ts` dengan Credentials Provider memanggil `authApi.login({ usernameOrEmail, password, clientId = 'auth-admin' })`, menangani error spesifik backend (`ApiException`: 401, 403, 429), dan memetakan ke subclass `CustomAuthError` (`CredentialsSignin`).
  - Menjaga efisiensi cookie NextAuth (< 4096 bytes) dengan menyimpan `accessToken`, `refreshToken`, `accessTokenExpiresAt`, dan user ringkas di JWT token tanpa menduplikasi array `roles`/`permissions`.
  - Callback `session` mengekstrak `roles` dan `permissions` secara dinamis di sisi server melalui `decodeAccessTokenClaims(token.accessToken)` tanpa mengekspos raw token ke client.
  - Mengonfigurasi `proxy.ts` (Next.js 16 Middleware): bypass rute publik, proteksi rute tertutup dengan redirect ke `/auth/login?callbackUrl=...`, single-flight refresh token ke backend saat token mendekati kedaluwarsa (<= 60s) dengan penulisan ulang `Set-Cookie` ke response, dan penanganan session revocation.
  - Menyediakan helper otorisasi `lib/auth/permissions.ts` (`isSuperAdmin`, `hasPermission`, `requirePermission`) dan komponen UI `<Can permission="...">` di `components/auth/can.tsx` dengan bypass otomatis untuk `SuperAdmin`.
  - Mengimplementasikan helper `getServerAccessToken()` di `lib/api/auth-token.ts` dan menghubungkannya ke `lib/api/client.ts` sehingga panggilan API admin otomatis menyematkan Bearer token.
  - Menghubungkan alur logout ke backend `POST /api/v1/auth/logout` dengan `refreshToken` untuk mencabut token di MSSQL dan mencatat audit log, lalu menghapus session NextAuth.
  - Memperbarui halaman dan formulir login `components/auth/login-form.tsx` dengan feedback error dinamis (401, 403, 429, SessionExpired) dan komponen `components/ui/alert.tsx`.
- [x] **Fase F4 — Halaman Self-Service Publik (Forgot/Reset Password & Aktivasi Akun)** *(Selesai 100%)*
  - Mengimplementasikan alur `/auth/forgot-password` dengan server action `handleForgotPasswordAction` memanggil `authApi.forgotPassword`. Menampilkan pesan sukses generik untuk melindungi privasi akun sesuai aturan mitigasi enumerasi (`PLANNING_FRONTEND.md` Bagian 8.4).
  - Mengimplementasikan alur `/auth/reset-password?token=...` dengan validasi password baru dan konfirmasi (min 8 karakter) via `resetPasswordSchema` dan server action `handleResetPassword`. Menangani token tidak ada atau tidak valid/kedaluwarsa dari backend secara informatif. Menjaga kompatibilitas route lama `/auth/create-password`.
  - Mengimplementasikan alur `/auth/activate-account` dengan server action `handleRequestActivationAction` memanggil `authApi.requestActivation` dengan pesan sukses generik.
  - Mengimplementasikan alur `/auth/activate-account/confirm?token=...` yang otomatis memvalidasi token aktivasi ke backend `authApi.confirmActivation`, menampilkan status sukses (centang hijau) dengan navigasi ke login, atau status gagal (silang merah) dengan tombol meminta tautan aktivasi baru.
  - Verifikasi: `npm run lint` (0 error, 0 warning), `npm run build` (Turbopack 10/10 routes lulus bersih), dan `dotnet test` (11/11 tests lulus).
- [x] **Fase F5 — Profil & Ganti Password** *(Selesai 100%)*
  - Mengimplementasikan halaman `/profile` (Server Component) dengan breadcrumb navigasi, proteksi sesi, serta pemanggilan realtime `GET /api/v1/auth/me` (`authApi.getMe`).
  - Menampilkan ringkasan profil administrator (`components/profile/profile-overview.tsx`): identitas akun, salin User ID global ke clipboard, konteks aplikasi `auth-admin`, badge status (Aktif/Terverifikasi), daftar peran (Roles), dan izin efektif (Permissions) dengan penanda khusus SuperAdmin.
  - Memisahkan helper murni `isSuperAdmin` dan `hasPermission` ke `lib/auth/permission-rules.ts` agar komponen client tidak mengimpor modul `server-only`.
  - Mengimplementasikan form ganti password (`components/profile/change-password-form.tsx`) dengan validasi `changePasswordSchema` (min 8 karakter, konfirmasi cocok, tidak boleh sama dengan password saat ini).
  - Mengimplementasikan Server Action `handleChangePasswordAction` memanggil `authApi.changePassword`. Setelah perubahan sukses, sistem secara otomatis membersihkan sesi NextAuth lokal dan mengarahkan admin untuk login ulang sesuai aturan bahwa backend mencabut seluruh sesi aktif user.
  - Verifikasi: `npm run lint` (0 error, 0 warning), `npm run build` (Turbopack 11/11 routes lulus bersih), dan `dotnet test` (11/11 tests lulus).
- [x] **Fase F6 — Manajemen Aplikasi (`ClientApps`) & API Key** *(Selesai 100%)*
  - Mengimplementasikan Server Actions (`components/apps/actions/apps.ts`): `createAppAction`, `updateAppAction`, dan `rotateApiKeyAction` dengan proteksi izin (`requirePermission('apps.write')`, `requirePermission('apps.rotate-key')`), validasi Zod (`createClientAppSchema`, `updateClientAppSchema`), `revalidatePath`, dan proteksi pemblokiran penonaktifan aplikasi sistem `auth-admin`.
  - Mengimplementasikan modal sekali-tampil API Key (`components/apps/api-key-reveal-modal.tsx`) sesuai aturan keamanan Bagian 8.1: menampilkan `rawApiKey` hanya sekali dengan tombol salin, peringatan tegas, dan panduan header `X-Client-Id` & `X-Api-Key` tanpa menyimpannya ke state global/storage/cookie.
  - Mengimplementasikan dialog pendaftaran aplikasi baru (`components/apps/create-app-dialog.tsx`) dengan validasi format `clientId`, auto-lowercase, input CORS allowed origins, dan langsung membuka modal API Key setelah berhasil.
  - Mengimplementasikan tabel aplikasi (`components/apps/apps-table.tsx`) dan halaman `/apps` (`app/(dashboard)/apps/page.tsx`) dengan pencarian instan (nama/client id/deskripsi), filter status (semua/aktif/nonaktif), badge aplikasi sistem, salin Client ID, format tanggal Indonesia, dan pembungkusan tombol registrasi dengan `<Can permission="apps.write">`.
  - Mengimplementasikan halaman detail `/apps/[appId]` (`app/(dashboard)/apps/[appId]/page.tsx`) dengan breadcrumbs, header ringkasan, dan 4 tab:
    - Tab *Informasi* (`components/apps/app-info-tab.tsx`): pembaruan nama, deskripsi, CORS origins, switch status aktif yang dikunci untuk `auth-admin`, serta tombol simpan berizin.
    - Tab *API Key & Integrasi* (`components/apps/app-api-key-tab.tsx`): tampilan prefix kunci aktif, snippet integrasi backend M2M, peringatan destruktif rotasi, dan dialog konfirmasi rotasi kunci yang otomatis menampilkan modal kunci baru.
    - Tab *Permissions* & Tab *Roles*.
  - Verifikasi: `npm run lint` (0 error, 0 warning), `npm run build` (Turbopack 12/12 routes lulus bersih), dan `dotnet test` (11/11 tests lulus).
- [x] **Fase F7 — Permissions & Roles per Aplikasi** *(Selesai 100%)*
  - Memperbarui `lib/api/permissions.ts` dan `lib/api/roles.ts` agar parameter `token?: string` bersifat opsional (otomatis mengambil Bearer token dari sesi server jika tidak dikirim eksplisit).
  - Mengimplementasikan Server Actions untuk Permissions (`components/apps/actions/permissions.ts`): `createPermissionAction`, `updatePermissionAction`, dan `deletePermissionAction` dengan proteksi `requirePermission('permissions.write')`, validasi Zod berpola `modul.aksi` (`createPermissionSchema`, `updatePermissionSchema`), penanganan konflik 409, dan `revalidatePath`.
  - Mengimplementasikan Server Actions untuk Roles (`components/apps/actions/roles.ts`): `createRoleAction`, `updateRoleAction`, `deleteRoleAction`, dan `setRolePermissionsAction` dengan proteksi `requirePermission('roles.write')`, validasi Zod, serta proteksi terhadap `System Role` (melarang penghapusan dan perubahan nama `System Role`).
  - Mengimplementasikan Tab *Permissions* (`components/apps/app-permissions-tab.tsx`) pada `/apps/[appId]`: tabel permission, pencarian instan, filter per prefix modul (`modul.*`), salin kode permission, dialog tambah/ubah permission, dan dialog konfirmasi penghapusan permission (dengan `autoFocus` pada aksi aman "Batal").
  - Mengimplementasikan Tab *Roles* (`components/apps/app-roles-tab.tsx`) pada `/apps/[appId]`: tabel role dengan badge biru **System Role**, pencarian & filter tipe role (Semua/System Role/Custom), dialog tambah role (lengkap dengan pemilihan permission awal), dialog ubah role (input nama dikunci untuk `System Role`), tombol hapus yang dinonaktifkan untuk `System Role` beserta dialog konfirmasi hapus untuk custom role, serta dialog pemetaan permission berbentuk daftar checkbox yang dikelompokkan per modul (`Pilih Semua`, `Kosongkan`, `Pilih Modul Ini`).
  - Memperbarui Server Component `app/(dashboard)/apps/[appId]/page.tsx` untuk memuat daftar `permissions` dan `roles` secara paralel (`Promise.allSettled`) dan meneruskannya ke komponen tab terkait.
  - Verifikasi: `npm run lint` (0 error, 0 warning), `npm run build` (Turbopack 12/12 routes lulus bersih), dan `dotnet test` (11/11 tests lulus).
- [x] **Fase F8 — Manajemen User & Penugasan Akses** *(Selesai 100%)*
  - Memperbarui `lib/api/users.ts` dan `lib/api/user-app-roles.ts` agar parameter `token?: string` bersifat opsional dan menyelaraskan tipe balikan `usersApi.unlock` menjadi `Promise<AdminUserDto>` sesuai kontrak backend `AdminController.UnlockUser`.
  - Mengimplementasikan Server Actions (`components/users/actions/users.ts`): `createUserAction` (`users.write`), `updateUserAction` (`users.write`), `unlockUserAction` (`users.unlock`), `revokeUserSessionsAction` (`users.revoke-sessions`), `getUserAppAccessDetailAction` (`users.read`), dan `setUserAppRolesAction` (`users.write`) lengkap dengan validasi Zod dan `revalidatePath`.
  - Mengimplementasikan dialog pembuatan pengguna baru (`components/users/create-user-dialog.tsx`) dengan validasi username, email, nama lengkap, password + konfirmasi password, serta switch status aktif & verifikasi.
  - Mengimplementasikan halaman `/users` (`app/(dashboard)/users/page.tsx`) dan tabel pengguna (`components/users/users-table.tsx`) dengan pencarian server-side (`?search=...`), pagination (`?page=...`), badge status konsisten (*Aktif* hijau, *Nonaktif* abu-abu, *Terkunci* merah, *Belum Verifikasi* kuning), ringkasan akses aplikasi (`appAssignments`), dan navigasi ke detail user.
  - Mengimplementasikan halaman detail `/users/[userId]` (`app/(dashboard)/users/[userId]/page.tsx`) dengan 2 tab:
    - Tab *Profil & Keamanan Akun* (`components/users/user-info-tab.tsx`): form edit profil & status, dialog konfirmasi saat menonaktifkan pengguna, kartu proteksi brute-force dengan tombol **Buka Kunci & Reset Counter** (`Unlock`), serta kartu **Paksa Logout Semua Sesi** (`Revoke Sessions`) dengan dialog konfirmasi destruktif (`autoFocus` pada aksi aman "Batal").
    - Tab *Akses Aplikasi* (`components/users/user-app-access-tab.tsx`): ringkasan aplikasi yang sedang diakses user, pemilih aplikasi (`ClientApp`), daftar checkbox Role per aplikasi (lengkap dengan badge biru **System Role** & daftar kode permission), pratinjau realtime **Izin Efektif (Effective Permissions)**, serta tombol simpan penugasan role.
  - Verifikasi: `npm run lint` (0 error, 0 warning) dan `npm run build` (Turbopack 14/14 routes lulus bersih).
- [x] **Fase F9 — Audit Logs & Dashboard** *(Selesai 100%)*
  - Memperbarui `lib/api/audit-logs.ts` agar parameter `token?: string` bersifat opsional dan menyelaraskan nama query parameter tanggal (`dateFrom` & `dateTo`) sesuai signature `AdminController.GetAuditLogs`.
  - Mengimplementasikan katalog metadata 28 jenis `AuditEventType` (`components/audit-logs/audit-log-constants.ts`) beserta fungsi `prettyPrintDetailJson` untuk memformat payload JSON secara aman tanpa `dangerouslySetInnerHTML` (Bagian 8.6).
  - Mengimplementasikan dialog detail log audit (`components/audit-logs/audit-log-detail-dialog.tsx`) yang menampilkan metadata lengkap (Log ID, EventType, Actor/User dengan tautan ke `/users/[userId]`, ClientApp dengan tautan ke `/apps/[appId]`, IP Address, User-Agent, dan blok `<pre><code>` JSON pretty-printed dengan tombol salin).
  - Mengimplementasikan halaman `/audit-logs` (`app/(dashboard)/audit-logs/page.tsx`) dan tabel audit (`components/audit-logs/audit-logs-table.tsx`) dengan filter lengkap (`eventType`, `clientId`, `isSuccess`, `userId`, `dateFrom`, `dateTo`), pagination server-side, dan klik cepat filter per pengguna.
  - Mengimplementasikan Dashboard utama (`app/(dashboard)/page.tsx`) yang memeriksa izin admin (`apps.read`, `users.read`, `audit-logs.read`) sebelum memanggil endpoint terkait dan menyembunyikan kartu/grafik bagi admin yang tidak memiliki permission terkait:
    - Kartu statistik: Aplikasi Terdaftar (aktif/nonaktif), Total Pengguna SSO, Login Berhasil (24 Jam), dan Login Gagal / Lockout (24 Jam).
    - Grafik **ApexCharts** (`components/dashboard/login-activity-chart.tsx`) untuk tren aktivitas login berhasil vs gagal selama 7 hari terakhir.
    - Tabel **Aktivitas Keamanan Terbaru** (`components/dashboard/recent-audit-logs-card.tsx`) beserta dialog inspeksi detail JSON.
  - Verifikasi: `npm run lint` (0 error, 0 warning) dan `npm run build` (Turbopack 15/15 routes lulus bersih).
- [x] **Fase F10 — Hardening, Pengujian & Dokumentasi** *(Selesai 100%)*
  - **Security Headers Hardening (`next.config.ts`):** Mengonfigurasi `Content-Security-Policy` (CSP) ketat (`default-src 'self'`, `frame-ancestors 'none'`), `X-Frame-Options: DENY`, `X-Content-Type-Options: nosniff`, `Referrer-Policy: no-referrer`, dan `Permissions-Policy: camera=(), microphone=(), geolocation=(), browsing-topics=()`.
  - **Error & Loading UX:**
    - Memperbarui komponen error boundary root (`app/error.tsx`), dashboard (`app/(dashboard)/error.tsx`), dan auth (`app/auth/error.tsx`) dengan styling kartu profesional, icon AlertTriangle, pesan Bahasa Indonesia informatif, tombol "Coba Lagi" (`reset()`), dan tautan navigasi aman.
    - Membuat modul loading skeleton reusable (`components/shared/page-skeletons.tsx`): `TablePageSkeleton`, `DetailPageSkeleton`, dan `DashboardPageSkeleton`.
    - Menerapkan file `loading.tsx` pada rute dashboard: `app/(dashboard)/loading.tsx`, `/apps/loading.tsx`, `/apps/[appId]/loading.tsx`, `/users/loading.tsx`, `/users/[userId]/loading.tsx`, `/audit-logs/loading.tsx`, dan `/profile/loading.tsx`, serta merapikan `app/loading.tsx` root dengan dukungan dark mode.
  - **Pengujian Otomatis Minimal (Unit Testing):**
    - Membuat suite pengujian unit native Node.js (`node --test`) di `web/auth-admin-portal/tests/`:
      - `tests/validation.test.ts` (15 test): Menguji seluruh skema Zod (`auth.schema.ts`, `apps.schema.ts`, `permissions.schema.ts`, `roles.schema.ts`, `users.schema.ts`) termasuk panjang password 8-256 karakter, format `clientId`, pola permission `modul.aksi`, dan form user/role.
      - `tests/errors.test.ts` (9 test): Menguji pemetaan RFC 9457 `ProblemDetails` ke kelas `ApiException`, helper status boolean (`isUnauthorized`, `isForbidden`, `isNotFound`, `isConflict`, `isRateLimited`, `isServerError`), dan format pesan error `getErrorMessage`.
    - Menambahkan script `"test"` pada `package.json` (`npm run test`), lulus 24/24 tests (0 failures).
  - **Dokumentasi Lengkap (`web/auth-admin-portal/README.md`):** Menyusun panduan menyeluruh mencakup arsitektur BFF, security highlights, tabel variabel lingkungan, panduan setup lokal bersama backend `auth-api` (.NET 10 LTS), dokumentasi perintah (`npm run dev`, `build`, `lint`, `test`), matriks hak akses/permissions, dan struktur direktori.
  - **Verifikasi Akhir:**
    - `npm run lint` -> **Lulus bersih** (`0 errors, 0 warnings`).
    - `npm run test` -> **Lulus 100%** (24/24 unit tests lulus).
    - `npm run build` -> **Lulus bersih** (Turbopack compiled 15/15 routes successfully).
    - `dotnet test AuthApi.sln` -> **Lulus 100%** (11/11 tests lulus).

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
4. **Keamanan API Key Sekali Tampil (Bagian 8.1):**
   - Nilai `rawApiKey` dari pendaftaran aplikasi baru maupun rotasi kunci hanya ditampilkan satu kali pada komponen dialog modal `ApiKeyRevealModal` dan dibersihkan saat dialog ditutup.
   - Tidak pernah disimpan ke dalam state global, URL parameter, localStorage, sessionStorage, maupun cookie.
   - Backend hanya menyimpan hash SHA-256 dan prefix kunci `apiKeyPrefix`.
5. **Proteksi Aplikasi Sistem `auth-admin` & System Roles:**
   - Aplikasi dengan Client ID `auth-admin` dikunci agar tidak dapat dinonaktifkan baik melalui validasi Server Action maupun switch antarmuka pengguna, selaras dengan aturan backend.
   - Role dengan `isSystemRole = true` ditandai dengan badge biru **System Role**, serta tombol hapus dan input ubah nama dinonaktifkan di UI dan divalidasi di Server Action.
6. **Keamanan Tampilan Audit Log (Bagian 8.6):**
   - Payload `detailJson` pada Audit Log ditampilkan sebagai teks terformat (`JSON.stringify(..., null, 2)`) di dalam elemen `<pre><code>` tanpa `dangerouslySetInnerHTML`.
7. **Pengujian Unit Tanpa Dependensi Berat (Fase F10):**
   - Pengujian unit skema Zod dan parser ProblemDetails dijalankan menggunakan Node.js native test runner (`node --test`), menghasilkan eksekusi sangat cepat (< 300ms) tanpa dependensi testing framework eksternal.

---

## 3. Masalah yang Diketahui

1. **Tidak Ada Blocker:** Seluruh fase backend (Fase 1 s/d 8) dan frontend (Fase F0 s/d F10) telah selesai 100%. Seluruh pengujian otomatis backend (11 tests) dan frontend (24 tests) serta build produksi (Turbopack 15/15 routes) lulus bersih.

---

## 4. Langkah Berikutnya

1. **Menunggu Instruksi User:**
   - Melakukan commit ke Git lokal dan push ke remote branch `feature/fase-f7-permissions-and-roles` (atau branch baru), lalu membuat Pull Request (PR) ke `main`.
