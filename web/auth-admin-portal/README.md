# Auth Admin Portal (`web/auth-admin-portal`)

Antarmuka web resmi untuk **Centralized Auth API (`auth-api`)**, dibangun menggunakan **Next.js 16 (App Router)**, **React 19**, **TypeScript**, **Tailwind CSS v4**, **shadcn/ui**, dan **NextAuth v5 (BFF Pattern)**.

## Fitur Utama

1. **Admin Portal (Terproteksi):**
   - Manajemen Aplikasi Konsumen (`ClientApps`) & Rotasi API Key.
   - Manajemen Pengguna Global (`Users`), Status Akun, Unlock, dan Revoke Sessions.
   - Manajemen Role & Permission per Aplikasi Terdaftar.
   - Audit Logs Keamanan Lintas Aplikasi.
   - Dashboard Statistik & Pemantauan Keamanan.
2. **Self-Service Pengguna (Publik):**
   - Halaman Login SSO Admin (`/auth/login`).
   - Lupa Password (`/auth/forgot-password`).
   - Reset Password (`/auth/reset-password`).
   - Aktivasi Akun Pengguna Baru (`/auth/activate-account`).

## Menjalankan di Lokal

### Prasyarat
- Node.js LTS (v20+ / v22+) & npm.
- Backend `auth-api` (.NET 10) berjalan di `http://localhost:5100`.

### Instalasi & Menjalankan Server Dev

```bash
# Masuk ke folder portal
cd web/auth-admin-portal

# Salin konfigurasi environment
cp .env.example .env.local

# Install dependensi
npm install

# Jalankan server Next.js (port 3000)
npm run dev
```

Buka [http://localhost:3000](http://localhost:3000) di browser.
