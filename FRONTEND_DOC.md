# FRONTEND_DOC.md — Dokumentasi Lengkap Frontend `web/auth-admin-portal`

Dokumen ini merupakan panduan arsitektur, struktur kode, dan implementasi antarmuka pengguna (UI) pada portal **User Management (`web/auth-admin-portal`)**. Dokumen ini dirancang agar pengembang maupun model AI di masa depan dapat memelihara dan mengembangkan fitur baru dengan konsisten.

---

## 1. Spesifikasi Teknologi

| Komponen | Spesifikasi / Library |
| :--- | :--- |
| **Framework** | Next.js 16.1.1 (App Router + Server Components) |
| **Library UI** | React 19.2.3 |
| **Bahasa** | TypeScript 5.8.3 |
| **Styling** | Tailwind CSS v4 + tw-animate-css |
| **Komponen UI** | Radix UI Primitives + shadcn/ui pattern |
| **Form & Validasi** | React Hook Form + Zod 3.24 |
| **Autentikasi (BFF)**| NextAuth.js v5 (beta 29) & Credentials Provider |
| **Tema Antarmuka** | Dark / Light Mode (`next-themes`) |
| **Notifikasi Toast** | `react-hot-toast` |
| **Grafik & Visual** | ApexCharts (`react-apexcharts`) |
| **Icon** | `lucide-react` |

---

## 2. Pola Arsitektur BFF (Backend-for-Frontend)

Frontend ini mengimplementasikan pola **Backend-for-Frontend (BFF)**:
1. **Tidak Ada Kredensial di Browser:** Token rahasia (`accessToken`, `refreshToken`, `rawApiKey`) **tidak pernah** disimpan di `localStorage`, `sessionStorage`, maupun state JavaScript sisi klien.
2. **Cookie Terenkripsi `httpOnly`:** NextAuth v5 mengenkripsi sesi dan menyimpannya di cookie browser yang tidak dapat dibaca oleh script browser (kebal serangan XSS).
3. **Komunikasi Server-to-Server:** Saat browser memanggil Server Action atau membuka Server Component, server Next.js yang bertindak sebagai jembatan untuk memanggil backend `auth-api` (`http://127.0.0.1:5100` atau `AUTH_API_BASE_URL`) dengan menyertakan header `Authorization: Bearer <accessToken>` dan `X-Forwarded-For`.
4. **Ukuran Cookie Di Bawah 4 KB:** Cookie sesi hanya memuat metadata identitas dasar pengguna. Daftar `roles` dan `permissions` diekstrak secara realtime di sisi server dari payload `accessToken` menggunakan utilitas [`decodeAccessTokenClaims`](./web/auth-admin-portal/lib/auth/jwt-utils.ts).

---

## 3. Struktur Direktori Proyek

```
web/auth-admin-portal/
├── app/
│   ├── (dashboard)/             # Route Group halaman yang diproteksi autentikasi
│   │   ├── apps/                # Manajemen Client Apps & detail per-aplikasi
│   │   │   ├── [appId]/page.tsx # Detail aplikasi: Info, API Key, Roles, Permissions
│   │   │   └── page.tsx         # Tabel daftar aplikasi & pendaftaran app baru
│   │   ├── audit-logs/page.tsx  # Tabel jejak audit, filter event, dan detail log modal
│   │   ├── employees/           # Master pegawai, detail, dan lifecycle akun
│   │   ├── organization/offices/# Master kantor, struktur, unit, posisi, versi efektif
│   │   ├── profile/page.tsx     # Profil pengguna, ganti password, ringkasan organisasi
│   │   ├── users/               # Manajemen pengguna
│   │   │   ├── [userId]/page.tsx# Detail user: status, unlock, penugasan role per app
│   │   │   └── page.tsx         # Tabel daftar user & tambah user baru
│   │   ├── layout.tsx           # Layout dashboard (AppSidebar, Header, Breadcrumb)
│   │   └── page.tsx             # Halaman utama ringkasan/dashboard
│   ├── auth/                    # Route Group halaman publik & self-service
│   │   ├── activate-account/    # Permintaan tautan aktivasi akun
│   │   │   └── confirm/page.tsx # Konfirmasi kode/token aktivasi akun
│   │   ├── forgot-password/     # Permintaan link lupa password
│   │   ├── reset-password/      # Eksekusi reset password baru dengan token
│   │   ├── login/page.tsx       # Formulir login masuk portal
│   │   └── layout.tsx           # Layout split-panel auth (kiri: AuthIllustration)
│   ├── api/auth/[...nextauth]/  # Route handler NextAuth endpoint
│   ├── globals.css              # Pengaturan CSS utama & variabel Tailwind v4
│   └── layout.tsx               # Root layout HTML (Theme provider & Toaster)
│
├── components/
│   ├── apps/                    # Komponen fitur Client Apps & API Key modal
│   ├── audit-logs/              # Komponen tabel audit logs & modal JSON detail
│   ├── auth/                    # Form login, aktivasi, reset password, & AuthIllustration
│   ├── dashboard/               # Widget statistik dashboard & grafik aktivitas login
│   ├── employees/               # Tabel pegawai, CommandDialog generik, aksi, detail, lifecycle
│   ├── organization/            # Dialog master, tabel unit/posisi, pohon struktur
│   ├── layout/                  # Header, Breadcrumb, Footer, ProfileDropdown
│   ├── profile/                 # Form profil & ganti password
│   ├── shared/                  # LogoSidebar, ThemeLogo, SearchBox, ModeToggle, Skeletons
│   ├── ui/                      # Primitive komponen shadcn/ui (Button, Dialog, Table, dll)
│   ├── users/                   # Komponen tabel user, tab penugasan role, & modal tambah user
│   ├── app-sidebar.tsx          # Komponen Sidebar navigasi utama
│   └── sidebar-data.ts          # Struktur menu navigasi sidebar
│
├── lib/
│   ├── api/                     # Lapisan pemanggil backend API (client.ts, auth.ts, apps.ts, dll)
│   ├── auth/                    # Helper JWT claims, RBAC permissions rule, token refresh
│   ├── validation/              # Skema validasi Zod untuk seluruh formulir
│   └── utils.ts                 # Helper classNames (cn)
│
├── auth.ts                      # Konfigurasi inti NextAuth v5 & Credentials provider
└── proxy.ts                     # Handler rotasi refresh token di sisi server
```

---

## 4. Katalog Halaman & Alur Pengguna

### A. Halaman Publik & Self-Service (`/auth/*`)
Semua halaman publik menggunakan layout split-view:
- **Panel Kiri:** [`AuthIllustration.tsx`](./web/auth-admin-portal/components/auth/auth-illustration.tsx) menampilkan ilustrasi perisai keamanan SSO, kartu identitas `superadmin`, dan spesifikasi enkripsi token RS256.
- **Panel Kanan:** Form input dengan logo resmi [`ThemeLogo.tsx`](./web/auth-admin-portal/components/shared/theme-logo.tsx) (*User Management*).

1. **Masuk / Login (`/auth/login`):**
   - Mengambil input `usernameOrEmail` dan `password`.
   - Divalidasi di klien oleh `loginSchema` (Zod).
   - Menjalankan Server Action `handleLoginAction` yang memanggil `signIn("credentials", ...)`.
   - Mengarahkan ke halaman sebelumnya (`callbackUrl`) atau `/` jika sukses.
2. **Lupa Password (`/auth/forgot-password`):**
   - Mengirim email/username ke `POST /api/v1/auth/forgot-password`.
3. **Reset Password (`/auth/reset-password?token=...`):**
   - Memasukkan token dan password baru sesuai aturan keamanan password.
4. **Aktivasi Akun (`/auth/activate-account` & `/auth/activate-account/confirm`):**
   - Memvalidasi token aktivasi satu kali pakai (*one-time hashed token*).

---

### B. Halaman Terproteksi Dashboard (`/(dashboard)/*`)

1. **Dashboard Utama (`/`):**
   - Kartu ringkasan total aplikasi terdaftar, pengguna aktif, dan role.
   - Grafik interaktif aktivitas login harian (ApexCharts).
   - Tabel audit log keamanan terbaru.

2. **Manajemen Aplikasi (`/apps` & `/apps/[appId]`):**
   - **Tabel Aplikasi (`/apps`):** Menampilkan daftar Client Apps, Client ID, status aktif, dan jumlah user terdaftar.
   - **Modal Tambah Aplikasi (`CreateAppDialog`):** Mendaftarkan aplikasi baru.
   - **Modal Kunci API Sekali Tampil (`ApiKeyRevealModal`):** Menampilkan `rawApiKey` tepat satu kali setelah aplikasi dibuat atau saat rotasi kunci. Nilai ini tidak disimpan di state persisten.
   - **Detail Aplikasi (`/apps/[appId]`):** Memiliki 4 tab:
     1. *Info Aplikasi:* Mengubah nama, deskripsi, CORS allowed origins, dan status aktif.
     2. *API Key:* Melihat prefix kunci dan tombol **Rotasi Kunci API** dengan dialog konfirmasi.
     3. *Permissions:* Tabel izin akses aplikasi, tambah permission baru, edit deskripsi, dan hapus.
     4. *Roles:* Tabel role per aplikasi, buat role baru, hapus role, dan dialog **Kelola Permissions** (checkbox izin yang dimiliki role tersebut).

3. **Manajemen Pengguna (`/users` & `/users/[userId]`):**
   - **Tabel Pengguna (`/users`):** Pencarian instan berdasarkan username/email/nama, filter status aktif, dan pagination.
   - **Modal Tambah User (`CreateUserDialog`):** Pendaftaran user baru dengan verifikasi password.
   - **Detail Pengguna (`/users/[userId]`):**
     - *Info User:* Edit email, nama lengkap, dan status akun.
     - *Buka Kunci (Unlock):* Tombol untuk membuka akun yang terkunci akibat proteksi brute-force.
     - *Cabut Sesi (Revoke Sessions):* Tombol darurat untuk membatalkan seluruh sesi login aktif pengguna tersebut di semua perangkat.
     - *Penugasan Akses Aplikasi:* Memilih aplikasi tertentu dari dropdown, lalu mencentang role mana saja yang diberikan ke pengguna untuk aplikasi tersebut.

4. **Audit Trail Keamanan (`/audit-logs`):**
   - Menampilkan log aktivitas otentikasi & otorisasi secara realtime.
   - Filter berdasarkan: Jenis Event (`AUTH_LOGIN_SUCCESS`, `AUTH_LOGIN_FAILED`, dll), Client ID, Status (Sukses/Gagal), dan rentang tanggal.
   - Tombol **Lihat Detail:** Membuka modal dialog untuk melihat payload JSON metadata event secara aman tanpa `dangerouslySetInnerHTML`.

5. **Profil Akun (`/profile`):**
   - Menampilkan informasi akun yang sedang login, daftar peran sistem, dan hak akses efektif.
   - Formulir ganti password mandiri dengan validasi password saat ini dan password baru.

6. **Pegawai (`/employees`, `/employees/[id]`, `/employees/lifecycle`):**
   - **Daftar (`/employees`):** cari NIP/nama, paginasi, tambah pegawai (`employees.write`).
   - **Detail (`/employees/[id]`):** tab *Penempatan*, *Plh./Plt.*, *Status*, dan *Riwayat revisi*. Aksi: ubah identitas, ubah status (Aktif/Resign), tambah penempatan, mutasi/promosi, akhiri penempatan, tambah dan akhiri Plh./Plt. Setiap aksi membawa ETag pegawai yang sedang tampil sebagai `If-Match`; bila basi (412) pengguna diminta memuat ulang. UI membedakan *belum ditempatkan*, *vacant*, jadwal masa depan, dan penugasan yang sudah berakhir. Penempatan yang sudah berakhir tidak dapat menjadi sumber mutasi atau dasar acting.
   - **Lifecycle akun (`/employees/lifecycle`, `employees.lifecycle.write`):** pemeriksaan *dry-run* akun yang terdampak resign berlaku, lalu eksekusi dengan konfirmasi eksplisit (menonaktifkan akun dan mencabut sesi). Role dan akses aplikasi tidak diubah.
   - **Tab Organisasi pada `/users/[userId]`:** tautkan atau lepas akun ke pegawai (`employees.link.write` + `users.write`); dimuat hanya untuk pemegang `employees.read`.
   - **Tab Organisasi pada `/profile`:** ringkasan penempatan sendiri (hanya baca) dari `/auth/me/organization`.
   - Pesan yang selalu ditampilkan: perubahan kepegawaian **tidak mengubah role atau permission** akun.

7. **Master Organisasi (`/organization/offices`, `/organization/offices/[id]`):**
   - **Daftar kantor:** filter klasifikasi, tanggal efektif (`asOf`), tambah kantor (kode tepat 3 digit; `organization.write`).
   - **Detail kantor:** tab *Struktur* (pohon unit dan posisi per tanggal dengan penanda vacant), *Unit*, *Posisi*, dan *Versi efektif*. Tombol *Versi baru* pada kantor, unit, dan posisi menambah versi bertanggal efektif (mulai hari ini sampai 5 tahun ke depan); versi lama tetap tersimpan. Backend memeriksa seluruh struktur kantor di semua periode dan penempatan pegawai terdampak; perubahan yang tidak sah dibatalkan seluruhnya.
   - Tab *Unit* dan *Posisi* selalu menampilkan keadaan **hari ini**, tidak mengikuti tanggal pada tab *Struktur*. Tombol *Versi baru* dinonaktifkan untuk baris yang belum berlaku.
   - Versi baru tidak dapat ditambahkan bila sudah ada versi terjadwal di masa depan untuk data yang sama (pesan akan menjelaskannya). Menonaktifkan/mengarsipkan ditolak selama masih ada anak aktif atau pegawai yang ditempatkan.
   - Pimpinan unit wajib berkapasitas tepat 1; kapasitas kosong berarti tidak dibatasi.
   - Semua tindakan tulis pada master memerlukan permission `organization.write`, yang pada rilis awal hanya dimiliki SuperAdmin internal (D-06) dan tidak di-seed otomatis.
   - Level jabatan dan template struktur belum tersedia (menunggu keputusan D-05).

---

## 5. Kontrol Akses Berbasis Komponen (`<Can />`)

Untuk membatasi elemen antarmuka berdasarkan hak akses pengguna, gunakan komponen [`Can`](./web/auth-admin-portal/components/auth/can.tsx):

```tsx
import { Can } from "@/components/auth/can";

// Membatasi tombol hanya untuk role SuperAdmin
<Can role="SuperAdmin">
  <Button variant="destructive">Hapus Sistem</Button>
</Can>

// Membatasi tombol berdasarkan permission tertentu
<Can permission="user.create">
  <Button onClick={() => setOpen(true)}>Tambah Pengguna</Button>
</Can>
```

---

## 6. Variabel Lingkungan (*Environment Variables*)

File konfigurasi `.env` pada folder `web/auth-admin-portal`:

```env
# 1. Mode Lingkungan
NODE_ENV=production

# 2. NextAuth v5 Keamanan & Sesi
AUTH_SECRET=kunci_rahasia_minimal_32_karakter_acak
NEXTAUTH_SECRET=kunci_rahasia_minimal_32_karakter_acak
AUTH_URL=http://192.168.1.50:3000
NEXTAUTH_URL=http://192.168.1.50:3000
AUTH_TRUST_HOST=true

# 3. Koneksi ke Backend auth-api
AUTH_API_BASE_URL=http://127.0.0.1:5100
AUTH_API_CLIENT_ID=auth-admin
```

> **Catatan Penting Deploy Intranet/IP:**
> - `AUTH_TRUST_HOST=true` wajib diaktifkan agar NextAuth tidak menolak akses berbasis IP lokal (`UntrustedHost`).
> - `AUTH_API_BASE_URL` disarankan menggunakan `http://127.0.0.1:5100` jika frontend dan backend berada di VM yang sama.

---

## 7. Panduan Menambah Halaman atau Fitur Baru

Untuk menambahkan halaman atau fitur baru di dashboard:

1. **Definisikan Tipe Data & API Call:**
   - Tambahkan fungsi pemanggil di `lib/api/[fitur].ts` menggunakan wrapper `apiFetch<T>(endpoint, options)`.
2. **Buat Skema Validasi Form (Jika ada input pengguna):**
   - Tambahkan skema Zod di `lib/validation/[fitur].schema.ts`.
3. **Buat Server Actions:**
   - Letakkan di `components/[fitur]/actions/[namaAction].ts`.
   - Gunakan directive `"use server"` di baris paling atas.
   - Panggil fungsi API dan sertakan `revalidatePath("/route-terkait")`.
4. **Buat Halaman di App Router:**
   - Buat folder baru di `app/(dashboard)/[nama-fitur]/page.tsx`.
   - Ambil data di Server Component secara asinkron (`async function Page()`).
   - Sertakan file `loading.tsx` untuk menampilkan kerangka skeleton saat data dimuat.
5. **Tambahkan Menu di Sidebar:**
   - Buka `components/sidebar-data.ts`, tambahkan rute dan icon ke dalam array `navMain`.
6. **Verifikasi:**
   - Jalankan pengujian unit: `npm run test`
   - Jalankan linter: `npm run lint`
   - Jalankan type checking: `npx tsc --noEmit`

