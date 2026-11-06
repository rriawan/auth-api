# STATUS.md — Catatan Progres & Memori Kerja `auth-api`

> **Instruksi untuk Model AI:**  
> Ikuti `AGENTS.md` §§1, 8, dan 9 untuk konteks awal dan pembaruan. Hanya Agen Utama yang memperbarui file ini.

**Terakhir Diperbarui:** 2026-11-06

---

## 1. Sedang Dikerjakan

- **Planning Organization & Employee Profile (2026-11-06, keputusan final):** Pengguna menyepakati authorization terpusat existing tetap dipertahankan. Dokumen baru [`docs/PLANNING_ORGANIZATION_EMPLOYEE_PROFILE.md`](./docs/PLANNING_ORGANIZATION_EMPLOYEE_PROFILE.md) selesai disusun dan direview independen. Profil organisasi merupakan referensi; tidak otomatis mengubah role/permission, scope data atau approver. Belum mengubah source code, membuat migration, menjalankan aplikasi, atau mengakses database. Angka build/test existing di bawah merupakan catatan verifikasi sebelumnya, bukan hasil eksekusi sesi planning.
  - [x] Analisis existing dan aturan bisnis, rancangan data/API/portal, master 28 kantor, tahapan dan acceptance criteria.
  - [x] Review independen keamanan/konsistensi; koreksi unlink menjelang resign, semantik asOf link current, serta snapshot profil/ETag.
  - [ ] P0 — Konfirmasi keputusan D-01 s.d. D-08 dan kontrak.
  - [ ] P1 — Persistence dan master.
  - [ ] P2 — Profil dan penempatan.
  - [ ] P3 — API dan keamanan.
  - [ ] P4 — Siklus hidup akun.
  - [ ] P5 — Portal.
  - [ ] P6 — Pilot konsumen.
  - [ ] P7 — Hardening dan dokumentasi.

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

- **Kesepakatan final pengguna (menggantikan arah sebelumnya):** Pertahankan AuthN dan RBAC terpusat existing, termasuk syarat role aplikasi pada login/refresh/verify, JWT role/permission, serta pengelolaan akses portal. Tidak membuat UserClientApps pengganti. Tambahkan profil pegawai/organisasi opsional; mutasi/promosi/acting tidak mengubah role otomatis. Scope transaksi dan approval tetap milik konsumen. Blueprint baru melengkapi, bukan mengganti, PLANNING.md/PLANNING_FRONTEND.md.
- **Rekomendasi blueprint yang masih menunggu gate bisnis:** Link current opsional 1:1; asOf profil user merekonstruksi organisasi pegawai yang tertaut saat ini, bukan histori identitas akun. Profil terpisah dari JWT/session. Usulan unlink/relink ditolak bila ada status pembatas confirmed sekarang/masa depan; transaksi baca konsisten untuk profil dan ETag. Detail keputusan blocking tercatat D-01 s.d. D-08 pada dokumen baru.

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

1. **Ekstensi belum diimplementasikan:** Model pegawai/organisasi, API, guard baru, lifecycle, dan portal masih berupa planning. Authorization existing sengaja tetap dipertahankan. Scope kantor/unit pada grant dan role otomatis dari organisasi bukan target pengembangan.
2. **Batas revokasi existing:** Verifikasi realtime membaca akses terbaru, tetapi filter admin memakai klaim JWT. Belum ada access version pada JWT dan pengecekan versinya. Pencabutan refresh token tidak otomatis membatalkan JWT yang masih valid pada konsumen dengan validasi lokal saja.
3. **Keputusan bisnis terbuka:** D-01 s.d. D-08 mengatur sumber master, relasi akun–pegawai/nama, status/resign, tanggal efektif, kapasitas/rangkap/acting, struktur pusat, akses data, SLA/scheduler dan retensi. BRD tetap DRAFT. Struktur pusat/induk kantor tidak boleh dikarang; approval/fallback tetap tanggung jawab konsumen.
4. **Ketidaksesuaian catatan SDK:** `global.json` saat inspeksi berisi `10.0.100` dengan `latestFeature`, sedangkan catatan keputusan sebelumnya menyebut `10.0.403`. SDK yang benar-benar terpasang tidak diverifikasi pada sesi analisis.

---

## 4. Langkah Berikutnya

1. Review dokumen planning baru bersama pemilik proyek dan konfirmasi D-01 s.d. D-08 sesuai gate fase; jangan menganggap rekomendasi sebagai SOP yang telah disahkan.
2. Setelah instruksi implementasi diberikan, kerjakan P0 lalu fase ekstensi berikutnya satu per satu. Pertahankan seluruh AuthN/RBAC existing. Operasi database/migration apply/provisioning hanya oleh pengguna; verifikasi SQL Server juga dijalankan pengguna. Rencana policy engine organisasi dan pemindahan RBAC ke setiap konsumen tidak dilanjutkan.
3. Pengguna telah meminta publikasi dokumen planning dan status terkait ke GitHub pada 6 November 2026. Implementasi source code dan migration belum diminta. Operasi database tetap dilakukan manual oleh pengguna sesuai `AGENTS.md` §7.
