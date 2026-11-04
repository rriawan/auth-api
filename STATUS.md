# STATUS.md — Catatan Progres & Memori Kerja `auth-api`

> **Instruksi untuk Model AI:**  
> Baca file ini bersama `AGENTS.md`, `PLANNING.md`, `PLANNING_FRONTEND.md`, `BACKEND_DOC.md`, dan `FRONTEND_DOC.md` setiap kali memulai sesi baru. Perbarui file ini setiap kali menyelesaikan sebuah fase atau langkah penting agar sesi berikutnya dapat langsung melanjutkan tanpa kehilangan konteks.

**Terakhir Diperbarui:** 2026-11-04

---

## 1. Sedang Dikerjakan

- **Backend (`PLANNING.md` & `BACKEND_DOC.md`):** Seluruh tahapan pengembangan **Fase 1 s/d Fase 8** telah **selesai 100%** dan telah di-merge ke branch `main`.
  - **Status Build:** Lulus bersih (`dotnet build AuthApi.sln` -> `0 Warning(s)`, `0 Error(s)`).
  - **Status Pengujian Otomatis:** Lulus 100% (`dotnet test AuthApi.sln` -> `Passed: 11, Failed: 0, Skipped: 0`).
  - **Dokumentasi Endpoint:** Lengkap dan terdokumentasi di [`BACKEND_DOC.md`](./BACKEND_DOC.md).
- **Frontend (`PLANNING_FRONTEND.md` & `FRONTEND_DOC.md`):** Seluruh tahapan pengembangan **Fase F0 s/d Fase F10** telah **selesai 100%**.
  - **Status Linting:** Lulus bersih (`npm run lint` -> `0 problems`, `0 errors`, `0 warnings`).
  - **Status Pengujian Unit:** Lulus 100% (`npm run test` -> `Passed: 24, Failed: 0, 0 cancelled, 0 skipped`).
  - **Status TypeScript Check:** Lulus 100% (`tsc --noEmit` -> `0 errors`).
  - **Status Build Produksi:** Lulus bersih.
  - **Dokumentasi UI:** Lengkap dan terdokumentasi di [`FRONTEND_DOC.md`](./FRONTEND_DOC.md).

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
- [x] **Fase F1 — Inisialisasi Proyek `web/auth-admin-portal` dari Template** *(Selesai 100%)*
- [x] **Fase F2 — Lapisan API Client (`lib/api`) & Tipe Kontrak** *(Selesai 100%)*
- [x] **Fase F3 — Autentikasi Portal (NextAuth v5 + Rotasi Token di `proxy.ts`) & Proteksi Route** *(Selesai 100%)*
- [x] **Fase F4 — Halaman Self-Service Publik (Forgot/Reset Password & Aktivasi Akun)** *(Selesai 100%)*
- [x] **Fase F5 — Profil & Ganti Password** *(Selesai 100%)*
- [x] **Fase F6 — Manajemen Aplikasi (`ClientApps`) & API Key** *(Selesai 100%)*
- [x] **Fase F7 — Permissions & Roles per Aplikasi** *(Selesai 100%)*
- [x] **Fase F8 — Manajemen User & Penugasan Akses** *(Selesai 100%)*
- [x] **Fase F9 — Audit Logs & Dashboard** *(Selesai 100%)*
- [x] **Fase F10 — Hardening, Pengujian & Dokumentasi** *(Selesai 100%)*
- [x] **Penyegaran Branding & Desain Visual Portal (`User Management`)** *(Selesai 100%)*
  - Mengganti visual halaman login & self-service dari gambar template lama menjadi komponen vektor modern bertema Single User Pool SSO & RS256 JWT (`AuthIllustration`).
  - Mengganti logo "WowDash" dan icon heksagon lama dengan badge icon perisai keamanan (`ShieldCheck`) dan teks resmi **"User Management"** pada logo publik (`ThemeLogo`) dan sidebar dashboard (`LogoSidebar`).
- [x] **Hardening Produksi & Otorisasi Host NextAuth v5:**
  - Menambahkan opsi `trustHost: true` dan `secret` eksplisit di `auth.ts` untuk mendukung deployment Windows Server / IP LAN tanpa error `UntrustedHost`.

---

## 2. Keputusan Penting

1. **Target Framework & SDK Backend:** Seluruh 5 project (`Domain`, `Application`, `Infrastructure`, `WebApi`, dan `Tests`) menggunakan `.NET 10 LTS` (`net10.0`) dengan SDK `10.0.403` yang dikunci melalui `global.json` (`rollForward: latestFeature`).
2. **Isolasi Pengujian dari Database MSSQL (`AGENTS.md` Bagian 7):** Project `AuthApi.Tests` menggunakan `Microsoft.EntityFrameworkCore.InMemory` dengan nama database unik per test instance sehingga eksekusi `dotnet test` tidak pernah menyentuh server MSSQL.
3. **Arsitektur Frontend (`PLANNING_FRONTEND.md`):**
   - Ditempatkan secara monorepo pada folder `web/auth-admin-portal` menggunakan **Next.js 16 (App Router) + React 19 + Tailwind CSS v4 + shadcn/ui** mengacu pada `rriawan/TEMPLATE_NEXT`.
   - Menggunakan pola **BFF (Backend-for-Frontend)** dengan **NextAuth v5 Credentials Provider** (`clientId = auth-admin`).
   - Ukuran cookie session dijaga di bawah `4096 bytes` dengan tidak menduplikasi array `roles`/`permissions` di dalam cookie (diekstrak dari `accessToken` di sisi server).
   - Menetapkan `trustHost: true` eksplisit di `auth.ts` untuk deployment intranet/IP lokal.
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
8. **Atribut Validasi DataAnnotations pada Positional Records (C#):**
   - Dilarang menggunakan prefix target `[property: ...]` pada atribut validasi DataAnnotations (`[Required]`, `[MaxLength]`, `[EmailAddress]`, dsb.) pada parameter primary constructor record DTO.
9. **Dokumentasi Lengkap:**
   - [`BACKEND_DOC.md`](./BACKEND_DOC.md): Referensi teknis seluruh endpoint backend untuk pengembang / AI model masa depan.
   - [`FRONTEND_DOC.md`](./FRONTEND_DOC.md): Referensi arsitektur, halaman, pola BFF, dan komponen antarmuka pengguna portal web.
   - [`INTEGRATION_GUIDE.md`](./INTEGRATION_GUIDE.md): Panduan integrasi step-by-step untuk aplikasi konsumen baru.

---

## 3. Masalah yang Diketahui

1. **Tidak Ada Blocker:** Seluruh fitur telah terimplementasi dan terverifikasi dengan baik.

---

## 4. Langkah Berikutnya

1. Lakukan commit Git untuk menyimpan `BACKEND_DOC.md`, `FRONTEND_DOC.md`, dan pembaruan `auth.ts`.
