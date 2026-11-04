# Panduan Integrasi Projek Baru dengan `auth-api` (Localhost & Production)

Dokumen ini berisi panduan langkah demi langkah (*step-by-step*) untuk menghubungkan projek/aplikasi baru Anda ke layanan autentikasi dan otorisasi terpusat **`auth-api`** serta portal **User Management**.

---

## Daftar Isi
1. [Gambaran Cara Kerja](#1-gambaran-cara-kerja)
2. [Prasyarat Layanan Berjalan di Localhost](#2-prasyarat-layanan-berjalan-di-localhost)
3. [Tahap 1 — Mendaftarkan Aplikasi Baru di Portal User Management](#3-tahap-1--mendaftarkan-aplikasi-baru-di-portal-user-management)
4. [Tahap 2 — Konfigurasi CORS & Environment pada Projek Baru](#4-tahap-2--konfigurasi-cors--environment-pada-projek-baru)
5. [Tahap 3 — Implementasi Login, Refresh Token, dan Logout](#5-tahap-3--implementasi-login-refresh-token-dan-logout)
6. [Tahap 4 — Memvalidasi Token & Permission di Projek Baru](#6-tahap-4--memvalidasi-token--permission-di-projek-baru)
7. [Tahap 5 — Alur Self-Service (Aktivasi Akun, Lupa & Ganti Password)](#7-tahap-5--alur-self-service-aktivasi-akun-lupa--ganti-password)
8. [Daftar Kode Status & Penanganan Error](#8-daftar-kode-status--penanganan-error)

---

## 1. Gambaran Cara Kerja

- **Single User Pool (SSO Terpusat):** Satu akun pengguna yang terdaftar di `auth-api` dapat digunakan untuk login ke berbagai aplikasi/projek yang berbeda.
- **Isolasi Hak Akses per Aplikasi (`ClientApp`):** Meskipun akunnya satu, daftar `Role` dan `Permission` seorang pengguna diisolasi per aplikasi (`clientId`). Pengguna **wajib** memiliki minimal 1 Role aktif pada aplikasi tersebut agar diizinkan login.
- **JWT Asymmetric (RS256):** `accessToken` ditandatangani menggunakan RSA Private Key milik `auth-api`. Aplikasi baru Anda dapat memverifikasi keaslian token secara mandiri melalui endpoint JWKS atau memverifikasi secara realtime ke endpoint `/api/v1/auth/verify`.

---

## 2. Prasyarat Layanan Berjalan di Localhost

Pastikan kedua layanan berikut sudah berjalan di mesin lokal Anda:

1. **Backend `auth-api`** (`http://localhost:5100`):
   ```powershell
   dotnet run --project src/AuthApi.WebApi
   ```
   - Dokumentasi OpenAPI / Scalar UI dapat diakses di: `http://localhost:5100/scalar/v1`
   - Endpoint JWKS dapat diakses di: `http://localhost:5100/.well-known/jwks.json`

2. **Portal Admin User Management** (`http://localhost:3000`):
   ```powershell
   cd web/auth-admin-portal
   npm run dev
   ```

---

## 3. Tahap 1 — Mendaftarkan Aplikasi Baru di Portal User Management

Sebelum projek baru Anda dapat menggunakan endpoint login, daftarkan aplikasi baru Anda melalui Portal Admin (`http://localhost:3000`):

### Langkah 1.1: Buat Aplikasi Baru (`ClientApp`)
1. Login ke Portal Admin (`http://localhost:3000/auth/login`) menggunakan akun `SuperAdmin`.
2. Buka menu **Aplikasi (Client Apps)** di sidebar (`/apps`) -> Klik **Tambah Aplikasi**.
3. Isi formulir:
   - **Client ID:** Identitas unik aplikasi (gunakan huruf kecil, angka, atau strip `-`). Contoh: `inventory-app` atau `erp-portal`.
   - **Nama Aplikasi:** Nama tampilan aplikasi. Contoh: `Sistem Inventory Gudang`.
   - **Deskripsi:** Penjelasan singkat aplikasi.
4. Klik **Simpan**.
5. **PENTING — Simpan Raw API Key:**
   Setelah disimpan, modal **API Key** akan muncul **satu kali saja** (contoh: `ak_live_...`). Salin dan simpan nilai tersebut ke file `.env` projek baru Anda.

### Langkah 1.2: Buat Permission untuk Aplikasi Baru
1. Buka menu **Permissions** (`/permissions`).
2. Pilih aplikasi **`inventory-app`** pada dropdown pemilih aplikasi.
3. Klik **Tambah Permission** dan buat daftar izin sesuai fitur di projek baru Anda. Contoh:
   - `item.view` (*Melihat daftar barang*)
   - `item.create` (*Menambah barang baru*)
   - `item.delete` (*Menghapus barang*)
   - `report.export` (*Mengekspor laporan*)

### Langkah 1.3: Buat Role & Hubungkan Permission
1. Buka menu **Roles** (`/roles`).
2. Pilih aplikasi **`inventory-app`** pada dropdown.
3. Klik **Tambah Role** (contoh: `Admin Gudang` atau `Staff Gudang`).
4. Klik tombol **Kelola Permission** pada Role tersebut, lalu centang permission yang sesuai dan klik **Simpan**.

### Langkah 1.4: Berikan Akses User ke Aplikasi Baru
1. Buka menu **Pengguna (Users)** (`/users`).
2. Buat pengguna baru atau pilih pengguna yang sudah ada.
3. Buka detail pengguna tersebut -> Pada bagian **Penugasan Role Aplikasi**, pilih aplikasi **`inventory-app`**.
4. Centang Role yang diinginkan (misal: `Admin Gudang`) -> Klik **Simpan Role**.

> **Catatan Penting:** Jika pengguna belum ditugaskan ke Role mana pun pada `inventory-app`, maka percobaan login dengan `"clientId": "inventory-app"` akan ditolak dengan HTTP `403 Forbidden`.

---

## 4. Tahap 2 — Konfigurasi CORS & Environment pada Projek Baru

### A. Konfigurasi File `.env` pada Projek Baru Anda
Tambahkan konfigurasi berikut pada file `.env` / `appsettings.json` di projek baru Anda (misalnya projek baru berjalan di `http://localhost:3001` atau `http://localhost:5173`):

```env
AUTH_API_BASE_URL=http://localhost:5100
AUTH_CLIENT_ID=inventory-app
AUTH_CLIENT_API_KEY=isi_dengan_raw_api_key_dari_langkah_1_1
AUTH_JWKS_URL=http://localhost:5100/.well-known/jwks.json
```

### B. Pengaturan CORS pada `auth-api`
- **Jika Projek Baru Memanggil `auth-api` dari Sisi Server (BFF / Backend)** — seperti Next.js Server Actions, Route Handlers, NextAuth, Laravel, atau .NET Backend:
  Anda **tidak perlu** mengubah konfigurasi CORS karena request dilakukan antar-server.
- **Jika Projek Baru Memanggil `auth-api` Langsung dari Browser (SPA)** — seperti React Vite, Vue, atau Angular:
  Tambahkan origin localhost projek baru Anda ke dalam `src/AuthApi.WebApi/appsettings.Development.json`:
  ```json
  "Cors": {
    "AllowedOrigins": [
      "http://localhost:3000",
      "http://localhost:3001",
      "http://localhost:5173"
    ]
  }
  ```
  *(Restart `AuthApi.WebApi` setelah mengubah file konfigurasi tersebut).*

---

## 5. Tahap 3 — Implementasi Login, Refresh Token, dan Logout

### 5.1. Endpoint Login
- **URL:** `POST http://localhost:5100/api/v1/auth/login`
- **Headers:** `Content-Type: application/json`

**Request Body:**
```json
{
  "usernameOrEmail": "budi.santoso",
  "password": "Password123!",
  "clientId": "inventory-app"
}
```

**Contoh Kode (JavaScript / TypeScript):**
```ts
async function loginToAuthApi(usernameOrEmail: string, password: string) {
  const response = await fetch(`${process.env.AUTH_API_BASE_URL}/api/v1/auth/login`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify({
      usernameOrEmail,
      password,
      clientId: process.env.AUTH_CLIENT_ID, // "inventory-app"
    }),
  });

  if (!response.ok) {
    const problem = await response.json();
    throw new Error(problem.detail || problem.title || "Gagal login");
  }

  return await response.json();
}
```

**Response Sukses (`200 OK`):**
```json
{
  "accessToken": "eyJhbGciOiJSUzI1NiIsImtpZCI6...",
  "tokenType": "Bearer",
  "expiresInSeconds": 900,
  "expiresAtUtc": "2026-11-04T03:30:00Z",
  "refreshToken": "rT9x8...token_acak_panjang...",
  "refreshTokenExpiresAtUtc": "2026-11-11T03:15:00Z",
  "user": {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "username": "budi.santoso",
    "email": "budi@perusahaan.com",
    "fullName": "Budi Santoso",
    "clientId": "inventory-app",
    "isAccountVerified": true,
    "roles": ["Admin Gudang"],
    "permissions": ["item.view", "item.create", "item.delete", "report.export"]
  }
}
```

---

### 5.2. Endpoint Refresh Token Rotation (Perpanjang Sesi Otomatis)
Masa berlaku `accessToken` secara default adalah **15 menit** (`900` detik). Saat `accessToken` kedaluwarsa, tukarkan `refreshToken` untuk mendapatkan pasangan `accessToken` dan `refreshToken` yang baru.

- **URL:** `POST http://localhost:5100/api/v1/auth/refresh`
- **Headers:** `Content-Type: application/json`

**Request Body:**
```json
{
  "refreshToken": "rT9x8...token_acak_panjang...",
  "clientId": "inventory-app"
}
```

> **Peringatan Penting (Token Rotation & Reuse Detection):**
> Setiap kali `/api/v1/auth/refresh` berhasil dipanggil, `refreshToken` yang lama langsung **hangus (revoked)** dan digantikan oleh `refreshToken` baru di dalam response. Pastikan aplikasi Anda langsung menyimpan `accessToken` dan `refreshToken` yang baru. Jika `refreshToken` lama dipakai ulang, sistem keamanan akan mendeteksi indikasi pencurian token dan mencabut seluruh sesi pengguna tersebut.

---

### 5.3. Endpoint Profil Pengguna Saat Ini (`/me`)
Digunakan untuk mengambil profil pengguna beserta daftar role & permission dari `accessToken` aktif:
- **URL:** `GET http://localhost:5100/api/v1/auth/me`
- **Headers:** `Authorization: Bearer <accessToken>`

---

### 5.4. Endpoint Logout
Mencabut `refreshToken` di database agar sesi benar-benar berakhir:
- **URL:** `POST http://localhost:5100/api/v1/auth/logout`
- **Headers:**
  - `Authorization: Bearer <accessToken>`
  - `Content-Type: application/json`

**Request Body:**
```json
{
  "refreshToken": "rT9x8...token_acak_panjang..."
}
```

---

## 6. Tahap 4 — Memvalidasi Token & Permission di Projek Baru

Anda dapat memilih salah satu (atau kombinasi) dari **2 metode** berikut pada projek baru Anda:

### Metode A: Verifikasi Realtime Menggunakan Client API Key (Paling Mudah & Instan)
Cocok apabila Anda tidak ingin mengonfigurasi library kriptografi JWT di projek baru, atau membutuhkan kepastian status user & permission secara *real-time* langsung dari database `auth-api`.

#### 1. Verifikasi Token & Cek Permission Sekaligus
- **URL:** `POST http://localhost:5100/api/v1/auth/verify`
- **Headers:**
  - `Content-Type: application/json`
  - `X-Client-Id: inventory-app`
  - `X-Api-Key: <RAW_API_KEY_ANDA>`

**Request Body:**
```json
{
  "accessToken": "eyJhbGciOiJSUzI1NiIs...",
  "requiredPermission": "item.create",
  "requiredRole": null
}
```

**Response (`200 OK`):**
```json
{
  "isValid": true,
  "isAuthorized": true,
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "username": "budi.santoso",
  "email": "budi@perusahaan.com",
  "fullName": "Budi Santoso",
  "clientId": "inventory-app",
  "expiresAtUtc": "2026-11-04T03:30:00Z",
  "roles": ["Admin Gudang"],
  "permissions": ["item.view", "item.create", "item.delete", "report.export"],
  "reason": null
}
```
- Jika token kedaluwarsa/palsu: `"isValid": false`.
- Jika token valid tetapi user tidak punya permission `item.create`: `"isValid": true`, `"isAuthorized": false`.

#### 2. Ambil Daftar Permission Terbaru Milik User Tertentu
- **URL:** `GET http://localhost:5100/api/v1/auth/permissions/{userId}`
- **Headers:**
  - `X-Client-Id: inventory-app`
  - `X-Api-Key: <RAW_API_KEY_ANDA>`

---

### Metode B: Verifikasi JWT Lokal via JWKS (`/.well-known/jwks.json`)
Cocok untuk arsitektur *microservice* berkecepatan tinggi karena backend projek baru memvalidasi signature RSA256 secara lokal tanpa memanggil `auth-api` di setiap request.

- **Issuer:** `AuthApi` (sesuai `Jwt:Issuer` di `appsettings.json`)
- **Audience:** `inventory-app` (sama dengan `clientId` aplikasi Anda)
- **JWKS URL:** `http://localhost:5100/.well-known/jwks.json`

#### Contoh Konfigurasi di Projek Baru Berbasis **.NET (ASP.NET Core)**:
```csharp
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false; // Set true saat di Production (HTTPS)
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = "AuthApi",
            ValidateAudience = true,
            ValidAudience = "inventory-app",
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            RoleClaimType = "role"
        };

        // Ambil kunci publik RSA dari endpoint JWKS auth-api
        options.Configuration = new OpenIdConnectConfiguration
        {
            JwksUri = "http://localhost:5100/.well-known/jwks.json"
        };
    });
```

#### Contoh Verifikasi di Projek Baru Berbasis **Node.js / Next.js (`jose`)**:
```ts
import { createRemoteJWKSet, jwtVerify } from "jose";

const JWKS = createRemoteJWKSet(
  new URL("http://localhost:5100/.well-known/jwks.json")
);

export async function verifyAccessToken(token: string) {
  const { payload } = await jwtVerify(token, JWKS, {
    issuer: "AuthApi",
    audience: "inventory-app",
  });

  // payload berisi: sub (userId), unique_name (username), email, role, permission
  return payload;
}
```

---

## 7. Tahap 5 — Alur Self-Service (Aktivasi Akun, Lupa & Ganti Password)

Projek baru Anda tidak perlu membangun halaman *Lupa Password* atau *Aktivasi Akun* sendiri. Anda cukup mengarahkan (*redirect*) pengguna ke halaman publik Portal **User Management**:

- **Lupa Password:** Arahkan link ke `http://localhost:3000/auth/forgot-password`
- **Aktivasi Akun:** Arahkan link ke `http://localhost:3000/auth/activate-account`

Namun, jika ingin membangun antarmuka ganti password di dalam aplikasi baru Anda bagi user yang sedang login, panggil endpoint berikut:
- **URL:** `POST http://localhost:5100/api/v1/auth/change-password`
- **Headers:**
  - `Authorization: Bearer <accessToken>`
  - `Content-Type: application/json`
- **Body:**
  ```json
  {
    "currentPassword": "PasswordLama123!",
    "newPassword": "PasswordBaru456!"
  }
  ```

---

## 8. Daftar Kode Status & Penanganan Error

Seluruh respons error dari `auth-api` menggunakan format standar **RFC 9457 (`ProblemDetails`)**:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.2",
  "title": "Kredensial Tidak Valid",
  "status": 401,
  "detail": "Username/email atau password salah."
}
```

| HTTP Status | Penyebab Umum pada Saat Integrasi | Solusi |
| :--- | :--- | :--- |
| `400 Bad Request` | Field wajib (`usernameOrEmail`, `password`, `clientId`) kosong atau format tidak valid. | Periksa payload JSON yang dikirim. |
| `401 Unauthorized` | Password salah, token kedaluwarsa, atau `X-Api-Key` salah saat memanggil `/verify`. | Cek kredensial atau panggil `/api/v1/auth/refresh`. |
| `403 Forbidden` | Akun belum diaktivasi, akun dinonaktifkan, atau **user belum diberi Role di `clientId` tersebut**. | Buka Portal Admin -> menu **Users** -> tugaskan Role pada aplikasi terkait. |
| `423 Locked` | Akun terkunci sementara karena salah memasukkan password 5 kali berturut-turut. | Tunggu durasi lockout selesai (15 menit) atau minta Admin klik **Unlock** di Portal Admin. |
| `429 Too Many Requests` | Melewati batas Rate Limiting (percobaan request terlalu cepat dari IP yang sama). | Tunggu sekitar 1 menit sebelum mencoba kembali. |
