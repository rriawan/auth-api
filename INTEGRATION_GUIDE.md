# Panduan Integrasi Projek Baru (`INTEGRATION_GUIDE.md`)

Dokumen ini menjelaskan langkah praktis bagi setiap projek atau aplikasi baru (.NET, Node.js, Go, Python, Web SPA, maupun Mobile) untuk terhubung ke layanan terpusat **`auth-api`** tanpa perlu membangun sistem autentikasi dan otorisasi dari nol.

---

## 1. Langkah Provisioning Projek Baru (Oleh Administrator)

Setiap kali Anda memulai projek/aplikasi baru (misalnya aplikasi **`erp-core`**), daftarkan aplikasi tersebut melalui endpoint Admin menggunakan `Bearer JWT` `auth-admin` atau header `X-Client-Id: auth-admin` + `X-Api-Key`:

### A. Daftarkan Aplikasi Baru (`POST /api/v1/admin/apps`)
```http
POST /api/v1/admin/apps
X-Client-Id: auth-admin
X-Api-Key: <RAW_ADMIN_API_KEY>
Content-Type: application/json

{
  "clientId": "erp-core",
  "name": "ERP Core System",
  "description": "Sistem ERP Utama Perusahaan",
  "allowedOrigins": "https://erp.perusahaan.co.id",
  "isActive": true
}
```
> **Penting:** Simpan nilai `rawApiKey` dari respons pembuatan aplikasi karena nilai asli API Key hanya ditampilkan **1 kali**.

### B. Buat Katalog Permission & Role untuk Aplikasi Baru
1. **Buat Permission (`POST /api/v1/admin/apps/{appId}/permissions`):**
   ```json
   { "code": "invoice.create", "description": "Membuat tagihan baru" }
   ```
2. **Buat Role beserta Pemetaan Permission (`POST /api/v1/admin/apps/{appId}/roles`):**
   ```json
   {
     "name": "FinanceStaff",
     "description": "Staf Keuangan",
     "permissionIds": ["<GUID_PERMISSION_INVOICE_CREATE>"]
   }
   ```
3. **Tugaskan User ke Role pada Aplikasi Baru (`PUT /api/v1/admin/users/{userId}/apps/{appId}/roles`):**
   ```json
   {
     "roleIds": ["<GUID_ROLE_FINANCE_STAFF>"]
   }
   ```

---

## 2. Alur Autentikasi di Sisi Frontend / Mobile

Aplikasi frontend atau mobile cukup memanggil endpoint publik `auth-api` dengan menyertakan `clientId` aplikasi tersebut:

### A. Login (`POST /api/v1/auth/login`)
```http
POST /api/v1/auth/login
Content-Type: application/json

{
  "usernameOrEmail": "budi",
  "password": "PasswordKuat!123",
  "clientId": "erp-core"
}
```
Respons mengembalikan:
- `accessToken`: Token JWT RS256 (umur pendek, default 15 menit) berisi klaim `sub`, `unique_name`, `email`, `aud` (`"erp-core"`), `roles`, dan `permissions` khusus untuk `erp-core`.
- `refreshToken`: Token sesi (default 14 hari) untuk rotasi token tanpa login ulang.
- `user`: Profil ringkas beserta daftar `roles` dan `permissions` user di aplikasi tersebut.

### B. Refresh Token Rotation (`POST /api/v1/auth/refresh`)
Saat `accessToken` mendekati kedaluwarsa, tukarkan `refreshToken` untuk mendapatkan pasangan `accessToken` + `refreshToken` baru:
```http
POST /api/v1/auth/refresh
Content-Type: application/json

{
  "refreshToken": "rt_...",
  "clientId": "erp-core"
}
```
> **Catatan Keamanan:** Setiap `refreshToken` hanya berlaku untuk **1 kali penukaran**. Simpan `refreshToken` terbaru dari respons. Jika token lama yang sudah pernah dirotasi dikirimkan kembali, `auth-api` otomatis mencabut seluruh sesi aktif pengguna tersebut (*Refresh Token Reuse Detection*).

---

## 3. Integrasi di Sisi Backend Projek Baru

Backend aplikasi konsumen memiliki **2 pilihan pola validasi** (dapat dikombinasikan):

### Pola A — Validasi JWT RS256 Lokal via JWKS (Direkomendasikan untuk Traffic Tinggi)

Backend projek baru memverifikasi signature JWT RS256 secara lokal menggunakan RSA Public Key dari endpoint publik:
- **JWKS Endpoint:** `GET https://<HOST_AUTH_API>/.well-known/jwks.json`

#### Contoh di Projek Baru ASP.NET Core (.NET 10)
```csharp
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = "https://auth.internal.local",
            ValidateAudience = true,
            ValidAudience = "erp-core", // Wajib sesuai dengan clientId aplikasi ini
            ValidateLifetime = true,
            NameClaimType = "unique_name",
            RoleClaimType = "roles"
        };

        // Mengambil Public Key secara otomatis dari endpoint JWKS auth-api
        options.MetadataAddress = "https://auth.internal.local/.well-known/openid-configuration";
        // Atau muat JsonWebKeySet langsung dari https://<HOST_AUTH_API>/.well-known/jwks.json
    });

// Contoh Policy Otorisasi Berbasis Permission di Projek Baru:
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("CanCreateInvoice", policy =>
        policy.RequireClaim("permissions", "invoice.create"));
```

#### Contoh di Projek Baru Node.js / Express (`jose`)
```javascript
import { createRemoteJWKSet, jwtVerify } from "jose";

const JWKS = createRemoteJWKSet(new URL("https://auth.internal.local/.well-known/jwks.json"));

export function requirePermission(requiredPermission) {
  return async (req, res, next) => {
    try {
      const token = (req.headers.authorization || "").replace(/^Bearer\s+/i, "");
      const { payload } = await jwtVerify(token, JWKS, {
        issuer: "https://auth.internal.local",
        audience: "erp-core",
      });

      const permissions = Array.isArray(payload.permissions)
        ? payload.permissions
        : payload.permissions ? [payload.permissions] : [];

      if (requiredPermission && !permissions.includes(requiredPermission)) {
        return res.status(403).json({ title: "Forbidden", detail: `Missing permission: ${requiredPermission}` });
      }

      req.user = payload;
      next();
    } catch {
      return res.status(401).json({ title: "Unauthorized" });
    }
  };
}
```

---

### Pola B — Validasi Realtime via Endpoint Introspection (`POST /api/v1/auth/verify`)

Untuk transaksi kritikal (misal persetujuan pencairan dana, penghapusan data sensitif, atau sistem yang tidak ingin memverifikasi JWT secara lokal), backend aplikasi baru cukup memanggil `POST /api/v1/auth/verify` dengan menyertakan `X-Client-Id` dan `X-Api-Key`:

```http
POST /api/v1/auth/verify
X-Client-Id: erp-core
X-Api-Key: ak_live_...
Content-Type: application/json

{
  "accessToken": "eyJhbGciOiJSUzI1NiIs...",
  "requiredPermission": "invoice.approve",
  "requiredRole": null
}
```

#### Contoh Respons (`200 OK`)
```json
{
  "isValid": true,
  "isAuthorized": true,
  "userId": "8f29e04d-45d2-4d30-a9bc-71ef209a1102",
  "username": "budi",
  "email": "budi@perusahaan.co.id",
  "fullName": "Budi Santoso",
  "clientId": "erp-core",
  "expiresAtUtc": "2026-11-03T05:00:00Z",
  "roles": ["FinanceManager"],
  "permissions": ["invoice.approve", "invoice.create", "invoice.view"],
  "reason": null
}
```
Pastikan backend Anda memeriksa **`isValid == true && isAuthorized == true`** sebelum mengeksekusi transaksi tersebut.

