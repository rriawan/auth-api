# STATUS.md — Catatan Progres & Memori Kerja `auth-api`

> **Instruksi untuk Model AI:**  
> Baca file ini bersama `AGENTS.md`, `PLANNING.md`, dan `PLANNING_FRONTEND.md` setiap kali memulai sesi baru. Perbarui file ini setiap kali menyelesaikan sebuah fase atau langkah penting agar sesi berikutnya dapat langsung melanjutkan tanpa kehilangan konteks.

**Terakhir Diperbarui:** 2026-11-03

---

## 1. Sedang Dikerjakan

- **Backend (`PLANNING.md`):** Seluruh tahapan pengembangan **Fase 1 s/d Fase 8** telah **selesai 100%** dan telah di-merge ke branch `main`.
  - **Status Build:** Lulus bersih (`dotnet build AuthApi.sln` -> `0 Warning(s)`, `0 Error(s)`).
  - **Status Pengujian Otomatis:** Lulus 100% (`dotnet test AuthApi.sln` -> `Passed: 8, Failed: 0, Skipped: 0`).
- **Frontend (`PLANNING_FRONTEND.md`):** Dokumen perencanaan `PLANNING_FRONTEND.md` untuk **Auth Admin Portal (`web/auth-admin-portal`)** berbasis Next.js 16 (mengacu pada template `rriawan/TEMPLATE_NEXT`) telah selesai disusun dan disempurnakan. Menunggu instruksi untuk mulai mengeksekusi **Fase F0**.

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
- [ ] **Fase F0 — Penyesuaian Backend untuk Pola BFF (`ForwardedHeaders` & `PortalBaseUrl`)**
- [ ] **Fase F1 — Inisialisasi Proyek `web/auth-admin-portal` dari Template**
- [ ] **Fase F2 — Lapisan API Client (`lib/api`) & Tipe Kontrak**
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

---

## 3. Masalah yang Diketahui

1. **Tidak Ada Blocker:** Seluruh kode backend telah di-merge ke `main` dan dokumen `PLANNING_FRONTEND.md` siap dieksekusi.

---

## 4. Langkah Berikutnya

1. **Menunggu Instruksi User:**
   - Memulai pengerjaan **Fase F0** sesuai `PLANNING_FRONTEND.md`, atau
   - Menyusun dokumen perencanaan terpisah untuk fitur tambahan yang ditangguhkan, atau
   - Melakukan commit pembaruan dokumen (`PLANNING_FRONTEND.md`, `AGENTS.md`, `STATUS.md`) ke repository jika diminta oleh User.
