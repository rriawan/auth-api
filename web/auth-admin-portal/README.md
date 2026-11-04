# Auth Admin Portal (`web/auth-admin-portal`)

Antarmuka web administrasi resmi dan portal self-service terpusat untuk **Centralized Auth API (`auth-api`)**, dibangun menggunakan **Next.js 16 (App Router)**, **React 19**, **TypeScript**, **Tailwind CSS v4**, **shadcn/ui**, dan **NextAuth v5 (BFF Pattern)**.

---

## 1. Ikhtisar & Arsitektur

Portal ini dirancang untuk beroperasi secara decoupled bersama backend **`auth-api` (.NET 10 LTS)** menggunakan pola **Backend-for-Frontend (BFF)**. Seluruh komunikasi browser dikelola oleh server Next.js melalui HTTP cookies `httpOnly` yang terenkripsi, sehingga token rahasia tidak pernah terekspos ke sisi klien.

```
+----------------------------------------------------------------------------------+
|                               Browser Klien Admin                                |
+----------------------------------------------------------------------------------+
                                  ▲ (Cookie httpOnly terenkripsi)
                                  ▼
+----------------------------------------------------------------------------------+
|                   Next.js 16 Auth Admin Portal (BFF Layer)                       |
|   - proxy.ts (Middleware: Route Protection & Single-Flight Token Rotation)       |
|   - Server Actions & Server Components                                           |
|   - API Client (Header Forwarding: X-Forwarded-For, User-Agent)                  |
+----------------------------------------------------------------------------------+
                                  ▲ (REST API JSON / Bearer Token)
                                  ▼
+----------------------------------------------------------------------------------+
|                        Backend auth-api (.NET 10 LTS)                            |
|   - Forwarded Headers Middleware (KnownProxies / KnownNetworks)                  |
|   - RS256 JWT, Refresh Token Rotation, Rate Limiting, Audit Logs, MSSQL EF Core  |
+----------------------------------------------------------------------------------+
```

### Keunggulan Arsitektur & Keamanan
1. **Pola Backend-for-Frontend (BFF):**
   - Browser tidak pernah menyimpan `accessToken`, `refreshToken`, maupun `rawApiKey` di `localStorage`, `sessionStorage`, atau cookie non-`httpOnly`.
   - Ukuran cookie session dijaga di bawah 4KB dengan mengekstrak `roles` dan `permissions` secara dinamis di sisi server melalui payload JWT.
2. **Rotasi Token Single-Flight (`proxy.ts`):**
   - Rotasi token yang mendekati kedaluwarsa (<= 60 detik) dilakukan di middleware Next.js (`proxy.ts`), memastikan cookie session baru selalu tertulis kembali ke browser (`Set-Cookie`) sebelum Server Components merender halaman. Hal ini mencegah *Refresh Token Reuse Detection* yang tidak disengaja.
3. **Kebijakan API Key Sekali Tampil:**
   - Kunci API klien (`rawApiKey`) hanya ditampilkan satu kali dalam dialog modal khusus saat pembuatan aplikasi atau rotasi kunci, lalu langsung dibersihkan dari memori.
4. **Penerusan Identitas Klien Asli (`X-Forwarded-For`):**
   - API client meneruskan IP asli dan User-Agent pengguna ke backend `auth-api`, sehingga Rate Limiter dan Audit Log mencatat alamat IP riil administrator, bukan IP server Next.js.
5. **Security Headers Lengkap:**
   - `Content-Security-Policy` (CSP) yang membatasi eksekusi skrip, sumber aset, dan melarang framing (`frame-ancestors 'none'`).
   - `X-Frame-Options: DENY` (mencegah serangan Clickjacking).
   - `X-Content-Type-Options: nosniff` (mencegah MIME sniffing).
   - `Referrer-Policy: no-referrer`.
   - `Permissions-Policy: camera=(), microphone=(), geolocation=(), browsing-topics=()`.
6. **Perlindungan terhadap Enumerasi Akun:**
   - Halaman publik (Lupa Password & Permintaan Aktivasi) selalu mengembalikan pesan sukses generik untuk melindungi privasi akun pengguna.
7. **Penyajian Data Audit Aman:**
   - Payload JSON log audit (`detailJson`) ditampilkan menggunakan format JSON terstruktur di elemen `<pre><code>` tanpa `dangerouslySetInnerHTML`.

---

## 2. Fitur Utama

### A. Dashboard & Pemantauan Keamanan
- **Kartu Statistik:** Aplikasi terdaftar (aktif/nonaktif), total pengguna global SSO, login berhasil 24 jam, dan insiden login gagal/lockout 24 jam.
- **Grafik Tren Login 7 Hari:** Visualisasi ApexCharts membandingkan tren autentikasi berhasil vs gagal.
- **Log Keamanan Terbaru:** Pemantauan cepat aktivitas autentikasi dan administrasi beserta dialog inspeksi detail event.
- **Role-based Widget Visibility:** Kartu dan grafik secara otomatis tersembunyi bagi admin yang tidak memiliki izin terkait (`apps.read`, `users.read`, `audit-logs.read`).

### B. Manajemen Aplikasi Konsumen (`ClientApps`)
- Pendaftaran aplikasi baru dengan format `clientId` (`^[a-z0-9-]+$`), allowed CORS origins, dan modal penyerahan `rawApiKey` sekali-tampil.
- Pencarian instan dan filter status (Semua / Aktif / Nonaktif).
- Halaman detail aplikasi terpadu dengan 4 tab:
  - **Informasi:** Pembaruan nama, deskripsi, allowed origins, dan status aktif (aplikasi sistem `auth-admin` dikunci dari penonaktifan).
  - **API Key & Integrasi:** Panduan header `X-Client-Id` & `X-Api-Key` untuk integrasi M2M, pratinjau prefix kunci aktif, dan dialog konfirmasi rotasi kunci.
  - **Permissions:** Pembuatan, pembaruan, dan penghapusan kode izin dengan format terstandarisasi `modul.aksi`.
  - **Roles:** Pembuatan role kustom, badge biru **System Role** (dilindungi dari penghapusan/perubahan nama), dan dialog pemetaan izin modular berbentuk matriks checkbox.

### C. Manajemen Pengguna Global (`Users`)
- Pendaftaran pengguna baru dengan username, email, nama lengkap, password (min 8 karakter), serta penentuan status awal (aktif & terverifikasi).
- Tabel pengguna dengan pencarian server-side, pagination, badge status terpadu (*Aktif*, *Nonaktif*, *Terkunci*, *Belum Verifikasi*), dan ringkasan aplikasi yang dapat diakses.
- Halaman detail pengguna:
  - **Profil & Keamanan:** Pembaruan profil, status akun, tombol **Buka Kunci Akun** (reset counter brute force), dan tombol **Paksa Logout Semua Sesi** (revokasi refresh token).
  - **Akses Aplikasi:** Pemilihan aplikasi klien, penugasan role per aplikasi, dan pratinjau realtime **Izin Efektif (Effective Permissions)**.

### D. Log Audit Keamanan (`/audit-logs`)
- Filter lengkap: 28 jenis `AuditEventType`, aplikasi klien, status berhasil/gagal, ID pengguna, dan rentang tanggal (`dateFrom` & `dateTo`).
- Dialog inspeksi log audit lengkap dengan metadata aktor, IP address, user-agent, dan payload JSON terformat beserta tombol salin.

### E. Akun Saya & Profil (`/profile`)
- Ringkasan identitas administrator, User ID global, daftar role, dan daftar izin efektif.
- Formulir ubah password dengan validasi kesesuaian dan otomatis logout setelah berhasil untuk menjaga konsistensi sesi di backend.

### F. Halaman Self-Service Publik
- **Login SSO Admin (`/auth/login`):** Penanganan pesan error spesifik dari backend (kredensial salah, akun terkunci, belum terverifikasi, rate limit 429).
- **Lupa Password (`/auth/forgot-password`):** Permintaan token reset password dengan pesan sukses generik.
- **Reset Password (`/auth/reset-password?token=...`):** Penggantian password baru menggunakan one-time hashed token dari backend.
- **Permintaan Aktivasi (`/auth/activate-account`):** Pengiriman ulang tautan aktivasi akun.
- **Konfirmasi Aktivasi (`/auth/activate-account/confirm?token=...`):** Verifikasi otomatis token aktivasi akun dengan umpan balik visual instan.

---

## 3. Variabel Lingkungan (Environment Variables)

Salin file `.env.example` ke `.env.local`:

```bash
cp .env.example .env.local
```

| Variabel Lingkungan | Deskripsi | Default Lokal |
| :--- | :--- | :--- |
| `AUTH_SECRET` | Kunci rahasia enkripsi session cookie NextAuth v5 (min 32 karakter acak). | *(generate via `openssl rand -base64 32`)* |
| `AUTH_URL` | Base URL kanonis antarmuka web portal Next.js. | `http://localhost:3000` |
| `AUTH_API_BASE_URL` | Base URL endpoint backend `auth-api` (.NET 10). | `http://localhost:5100` |
| `AUTH_API_CLIENT_ID`| Client ID yang digunakan portal saat mengautentikasi pengguna admin. | `auth-admin` |

> **Catatan Keamanan:** Dilarang menambahkan prefix `NEXT_PUBLIC_` pada variabel `AUTH_SECRET` dan `AUTH_API_BASE_URL`. File `.env.local` telah dikecualikan dari Git melalui `.gitignore`.

---

## 4. Panduan Menjalankan di Lokal

### Prasyarat
1. **.NET 10 SDK** (versi LTS `10.0.403` atau lini patch tertinggi) untuk backend `auth-api`.
2. **Node.js** (versi v20+, v22+, atau v24 LTS) dan **npm**.
3. **Microsoft SQL Server (MSSQL)** aktif dengan database `AuthDb`.

### Langkah 1 — Menjalankan Backend `auth-api`
Buka terminal baru di root repository:

```bash
# Pastikan migrasi database telah diterapkan oleh administrator
# Jalankan backend API pada port 5100
dotnet run --project src/AuthApi.WebApi
```

Pastikan endpoint Scalar/OpenAPI dapat diakses di `http://localhost:5100/scalar/v1` dan JWKS di `http://localhost:5100/.well-known/jwks.json`.

### Langkah 2 — Menjalankan Frontend Auth Admin Portal
Buka terminal terpisah di root repository:

```bash
# Masuk ke folder web portal
cd web/auth-admin-portal

# Salin konfigurasi environment jika belum ada
cp .env.example .env.local

# Install dependensi (bila baru pertama kali)
npm install

# Jalankan server Next.js pada mode development (port 3000)
npm run dev
```

Buka peramban di [http://localhost:3000](http://localhost:3000).

### Akun Administrator Default
- **Username:** `superadmin`
- **Password:** `Admin123!`
- **Client ID:** `auth-admin`

---

## 5. Perintah yang Tersedia (Scripts)

| Perintah | Deskripsi |
| :--- | :--- |
| `npm run dev` | Menjalankan server Next.js mode pengembangan dengan hot-reload. |
| `npm run build` | Melakukan kompilasi produksi aplikasi menggunakan Next.js Turbopack compiler. |
| `npm run start` | Menjalankan build produksi pada port lokal. |
| `npm run lint` | Menjalankan pemeriksaan kualitas kode menggunakan ESLint (Next.js & React 19 rules). |
| `npm run test` | Menjalankan unit test otomatis untuk skema Zod dan parser error ProblemDetails (Node.js test runner). |

---

## 6. Pengujian Otomatis (Unit Testing)

Pengujian unit menggunakan Node.js native test runner (`node --test`), menguji logika validasi dan penanganan error tanpa dependensi berat:

```bash
# Jalankan pengujian unit
npm run test
```

Cakupan pengujian mencakup:
1. **Skema Zod (`lib/validation/*.schema.ts`):**
   - Aturan kompleksitas password (panjang 8-256 karakter).
   - Validasi email dan username.
   - Pola format `clientId` (huruf kecil, angka, strip).
   - Pola format `permissionCode` bertingkat (`modul.aksi`).
   - Validasi form pendaftaran/pembaruan pengguna, role, dan aplikasi.
2. **ProblemDetails & ApiException (`lib/api/errors.ts`):**
   - Pemetaan spesifikasi RFC 9457 ProblemDetails ke kelas `ApiException`.
   - Helper status HTTP (`isUnauthorized`, `isForbidden`, `isNotFound`, `isConflict`, `isRateLimited`, `isServerError`).
   - Format pesan ramah pengguna untuk status 429 (Rate Limit) dan penanganan fallback error tak terduga.

---

## 7. Referensi Kode Izin (Permissions Matrix)

Berikut adalah daftar kode izin yang dikenali oleh portal untuk aplikasi `auth-admin`:

| Kode Izin | Deskripsi |
| :--- | :--- |
| `apps.read` | Melihat daftar aplikasi konsumen dan detail konfigurasinya. |
| `apps.write` | Mendaftarkan aplikasi baru dan memperbarui konfigurasi aplikasi. |
| `apps.rotate-key` | Melakukan rotasi API Key aplikasi konsumen. |
| `roles.read` | Melihat daftar role dan pemetaan izin role. |
| `roles.write` | Membuat, memperbarui, dan menghapus role kustom. |
| `permissions.read` | Melihat daftar izin terdaftar per aplikasi. |
| `permissions.write` | Menambah, mengedit, dan menghapus kode izin. |
| `users.read` | Melihat daftar pengguna global dan status akunnya. |
| `users.write` | Mendaftarkan pengguna baru, mengedit profil, dan menetapkan role aplikasi. |
| `users.unlock` | Membuka akun pengguna yang terkunci akibat proteksi brute force. |
| `users.revoke-sessions` | Mencabut seluruh refresh token aktif pengguna secara paksa. |
| `audit-logs.read` | Mengakses riwayat log audit keamanan lintas aplikasi. |

> **Peran `SuperAdmin`:** Pengguna dengan role `SuperAdmin` memiliki bypass otomatis pada seluruh pemeriksaan izin di atas.
