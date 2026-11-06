# BACKEND_DOC.md — Dokumentasi Lengkap Backend `auth-api`

Dokumen ini dirancang sebagai panduan referensi utama bagi pengembang maupun model AI untuk memahami arsitektur, cara kerja, serta spesifikasi seluruh endpoint yang tersedia pada backend **`auth-api`**.

---

## 1. Arsitektur & Struktur Solusi

Layanan `auth-api` dibangun dengan prinsip **Clean / Layered Architecture** menggunakan **.NET 10 LTS (`net10.0`)** dan **Entity Framework Core 10** dengan database **Microsoft SQL Server (MSSQL)**.

```
src/
├── AuthApi.Domain/          # Entitas domain murni (User, ClientApp, Role, Permission, AuditLog, RefreshToken)
├── AuthApi.Application/     # DTO, interface abstraksi, logika bisnis use-case, validasi
├── AuthApi.Infrastructure/  # EF Core DbContext, implementasi RSA RS256, PasswordHasher, AuditLogService
└── AuthApi.WebApi/          # Controllers (REST API), Middleware, Filter Rate Limit, OpenAPI Scalar
```

### Konsep Inti
1. **Single User Pool (SSO Terpusat):** 1 akun pengguna terdaftar global dapat digunakan untuk masuk ke berbagai aplikasi konsumen.
2. **Isolasi Otorisasi per Aplikasi (`ClientApp`):** Setiap aplikasi terdaftar memiliki daftar `Role` dan `Permission` mandiri. Pengguna wajib memiliki penugasan minimal 1 role aktif pada aplikasi tujuan agar diizinkan login.
3. **Kriptografi Asimetris (RS256):** `accessToken` ditandatangani menggunakan RSA Private Key dan dapat diverifikasi secara publik via endpoint JWKS `/.well-known/jwks.json`.
4. **Refresh Token Rotation:** Penggunaan ulang refresh token lama akan memicu sistem deteksi pembajakan token (*token reuse detection*) yang otomatis mencabut seluruh sesi pengguna tersebut.

---

## 2. Standar Header & Format Error

### Header Autentikasi
- **Bearer Token (User Context):** `Authorization: Bearer <accessToken>`
- **Client App Context (Inter-Service / Backend-to-Backend):**
  - `X-Client-Id: <clientId>` (contoh: `auth-admin`, `erp-core`)
  - `X-Api-Key: <rawApiKey>` (kunci rahasia aplikasi)

### Standar Respons Error (RFC 9457 `ProblemDetails`)
Semua respons error (4xx dan 5xx) memiliki struktur JSON standar:
```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.2",
  "title": "Kredensial Tidak Valid",
  "status": 401,
  "detail": "Username/email atau password salah.",
  "instance": "POST /api/v1/auth/login",
  "traceId": "00-08224e0446018fc4694ef79d5ee98992-05f20af81080bc80-00"
}
```

---

## 3. Katalog Endpoint Lengkap

---

### A. Autentikasi & Sesi Pengguna (`/api/v1/auth`)

#### 1. Login Pengguna
Melakukan autentikasi akun user dan memvalidasi izin akses user pada `clientId` yang diminta.
- **Method / URL:** `POST /api/v1/auth/login`
- **Auth:** Publik (Dilindungi Rate Limiter IP: 15 req/menit)
- **Request Body:**
  ```json
  {
    "usernameOrEmail": "superadmin",
    "password": "Password123!",
    "clientId": "auth-admin"
  }
  ```
- **Response `200 OK`:**
  ```json
  {
    "accessToken": "eyJhbGciOiJSUzI1NiIs...",
    "tokenType": "Bearer",
    "expiresInSeconds": 900,
    "expiresAtUtc": "2026-11-04T07:15:00Z",
    "refreshToken": "rT9x8...token_acak...",
    "refreshTokenExpiresAtUtc": "2026-11-18T07:00:00Z",
    "user": {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "username": "superadmin",
      "email": "admin@askridasyariah.co.id",
      "fullName": "Super Administrator",
      "clientId": "auth-admin",
      "isAccountVerified": true,
      "roles": ["SuperAdmin"],
      "permissions": ["app.view", "user.manage", "role.manage"]
    }
  }
  ```
- **Error Codes:**
  - `401 Unauthorized`: Password atau username salah.
  - `403 Forbidden`: Akun nonaktif, belum diaktivasi, atau belum memiliki Role pada aplikasi tersebut.
  - `423 Locked`: Akun terkunci karena gagal login 5 kali beruntun.

---

#### 2. Perpanjang Sesi (Refresh Token Rotation)
Menukarkan `refreshToken` lama untuk mendapatkan pasangan token baru. Token lama otomatis di-revoke.
- **Method / URL:** `POST /api/v1/auth/refresh`
- **Auth:** Publik
- **Request Body:**
  ```json
  {
    "refreshToken": "rT9x8...token_acak...",
    "clientId": "auth-admin"
  }
  ```
- **Response `200 OK`:** Format data sama seperti respons Login.
- **Error Codes:**
  - `401 Unauthorized`: Token tidak valid, kedaluwarsa, atau terdeteksi reuse (otomatis mencabut sesi).

---

#### 3. Logout
Mencabut status aktif refresh token di database.
- **Method / URL:** `POST /api/v1/auth/logout`
- **Auth:** `Authorization: Bearer <accessToken>` (Opsional jika membawa refreshToken di body)
- **Request Body:**
  ```json
  {
    "refreshToken": "rT9x8...token_acak..."
  }
  ```
- **Response `200 OK`:**
  ```json
  { "message": "Logout berhasil." }
  ```

---

#### 4. Ambil Profil Sendiri (`/me`)
Mengambil ringkasan akun dan izin akses yang melekat pada access token saat ini.
- **Method / URL:** `GET /api/v1/auth/me`
- **Auth:** `Authorization: Bearer <accessToken>`
- **Response `200 OK`:**
  ```json
  {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "username": "superadmin",
    "email": "admin@askridasyariah.co.id",
    "fullName": "Super Administrator",
    "clientId": "auth-admin",
    "isAccountVerified": true,
    "roles": ["SuperAdmin"],
    "permissions": ["app.view", "user.manage"]
  }
  ```

---

#### 5. Ganti Password
Mengubah password akun user yang sedang login.
- **Method / URL:** `POST /api/v1/auth/change-password`
- **Auth:** `Authorization: Bearer <accessToken>`
- **Request Body:**
  ```json
  {
    "currentPassword": "PasswordLama123!",
    "newPassword": "PasswordBaru456!"
  }
  ```
- **Response `200 OK`:**
  ```json
  { "message": "Password berhasil diperbarui." }
  ```

---

#### 6. Alur Aktivasi Akun
- **Request Kirim Ulang Token Aktivasi:**
  - `POST /api/v1/auth/activate-account/request`
  - Body: `{ "emailOrUsername": "budi.santoso" }`
  - Response: `{ "message": "Tautan aktivasi akun telah dikirimkan jika akun terdaftar." }`
- **Konfirmasi Aktivasi:**
  - `POST /api/v1/auth/activate-account/confirm`
  - Body: `{ "token": "token_hash_dari_email" }`
  - Response: `{ "message": "Akun berhasil diaktivasi. Silakan login." }`

---

#### 7. Alur Lupa / Reset Password
- **Request Link Reset Password:**
  - `POST /api/v1/auth/forgot-password`
  - Body: `{ "emailOrUsername": "budi.santoso" }`
  - Response: `{ "message": "Tautan reset password telah dikirimkan jika akun terdaftar." }`
- **Konfirmasi Reset Password:**
  - `POST /api/v1/auth/reset-password`
  - Body:
    ```json
    {
      "token": "token_hash_dari_email",
      "newPassword": "PasswordBaru123!"
    }
    ```
  - Response: `{ "message": "Password berhasil direset. Silakan login dengan password baru." }`

---

### B. Verifikasi Token & Integrasi Projek Konsumen

#### 1. Verifikasi Token Realtime
Digunakan oleh backend aplikasi konsumen (misal ERP/HRIS) untuk memvalidasi token dan mengecek hak akses.
- **Method / URL:** `POST /api/v1/auth/verify`
- **Auth:** Dapat diautentikasi dengan:
  - Header: `Authorization: Bearer <accessToken>` **ATAU**
  - Header: `X-Client-Id: <clientId>` dan `X-Api-Key: <rawApiKey>`
- **Request Body:**
  ```json
  {
    "accessToken": "eyJhbGciOiJSUzI1NiIs...",
    "requiredPermission": "item.create",
    "requiredRole": null
  }
  ```
- **Response `200 OK`:**
  ```json
  {
    "isValid": true,
    "isAuthorized": true,
    "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "username": "budi.santoso",
    "email": "budi@perusahaan.com",
    "fullName": "Budi Santoso",
    "clientId": "inventory-app",
    "expiresAtUtc": "2026-11-04T07:30:00Z",
    "roles": ["Staff Gudang"],
    "permissions": ["item.view", "item.create"],
    "reason": null
  }
  ```

---

#### 2. Ambil Permissions User Terbaru
- **Method / URL:** `GET /api/v1/auth/permissions?userId={guid}`
- **Auth:**
  - Header: `Authorization: Bearer <accessToken>` (otomatis mengambil user aktif) **ATAU**
  - Query param `userId` dengan Header: `X-Client-Id` dan `X-Api-Key`.
- **Response `200 OK`:**
  ```json
  {
    "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "username": "budi.santoso",
    "email": "budi@perusahaan.com",
    "clientId": "inventory-app",
    "isUserActive": true,
    "roles": ["Staff Gudang"],
    "permissions": ["item.view", "item.create"],
    "fetchedAtUtc": "2026-11-04T07:00:00Z"
  }
  ```

---

#### 3. Endpoint JWKS (Public Key RSA)
Digunakan oleh library JWT (seperti ASP.NET Core `AddJwtBearer` atau Node.js `jose`) untuk memvalidasi tanda tangan RS256 secara lokal tanpa memanggil API terus menerus.
- **Method / URL:** `GET /.well-known/jwks.json`
- **Auth:** Publik (Bebas akses)
- **Response `200 OK`:**
  ```json
  {
    "keys": [
      {
        "kty": "RSA",
        "use": "sig",
        "kid": "auth-api-rsa-key-1",
        "alg": "RS256",
        "n": "u1Z8...modulus...",
        "e": "AQAB"
      }
    ]
  }
  ```

---

### C. Administrasi Portal (`/api/v1/admin/*`)

> **Catatan Otorisasi:** Seluruh endpoint di bawah `/api/v1/admin/*` memerlukan header:
> `Authorization: Bearer <accessToken>` milik pengguna dengan role administrator/SuperAdmin.

---

#### 1. Manajemen Aplikasi Terdaftar (`ClientApps`)

- **Ambil Semua Aplikasi:**
  - `GET /api/v1/admin/apps`
  - Response: Array of `ClientAppDto` (daftar aplikasi, prefix API key, jumlah role/user).
- **Ambil Detail Aplikasi:**
  - `GET /api/v1/admin/apps/{id}`
- **Daftarkan Aplikasi Baru:**
  - `POST /api/v1/admin/apps`
  - Body:
    ```json
    {
      "clientId": "erp-system",
      "name": "ERP Enterprise",
      "description": "Sistem Utama ERP",
      "allowedOrigins": "https://erp.internal",
      "isActive": true
    }
    ```
  - Response `201 Created`: Mengembalikan `ClientAppWithRawKeyResponse` berisi `rawApiKey` (hanya tampil 1 kali).
- **Update Aplikasi:**
  - `PUT /api/v1/admin/apps/{id}`
  - Body: `{ "name": "...", "description": "...", "allowedOrigins": "...", "isActive": true }`
- **Rotasi API Key Aplikasi:**
  - `POST /api/v1/admin/apps/{id}/rotate-key`
  - Response `200 OK`: Mengembalikan API key baru `rawApiKey` (API key lama langsung hangus).

---

#### 2. Manajemen Permissions per Aplikasi

- **Daftar Permission pada Suatu Aplikasi:**
  - `GET /api/v1/admin/apps/{appId}/permissions`
- **Tambah Permission Baru:**
  - `POST /api/v1/admin/apps/{appId}/permissions`
  - Body: `{ "code": "report.export", "description": "Ekspor data ke Excel" }`
- **Update Permission:**
  - `PUT /api/v1/admin/apps/{appId}/permissions/{permissionId}`
  - Body: `{ "code": "report.export", "description": "Deskripsi baru" }`
- **Hapus Permission:**
  - `DELETE /api/v1/admin/apps/{appId}/permissions/{permissionId}`

---

#### 3. Manajemen Roles per Aplikasi

- **Daftar Role pada Suatu Aplikasi:**
  - `GET /api/v1/admin/apps/{appId}/roles`
- **Tambah Role Baru:**
  - `POST /api/v1/admin/apps/{appId}/roles`
  - Body:
    ```json
    {
      "name": "Supervisor",
      "description": "Role pengawas transaksi",
      "permissionIds": ["guid-permission-1", "guid-permission-2"]
    }
    ```
- **Update Info Role:**
  - `PUT /api/v1/admin/apps/{appId}/roles/{roleId}`
  - Body: `{ "name": "Supervisor Operasional", "description": "..." }`
- **Hapus Role:**
  - `DELETE /api/v1/admin/apps/{appId}/roles/{roleId}`
  - *(Catatan: System Role seperti `SuperAdmin` tidak dapat dihapus).*
- **Perbarui Daftar Permission Milik Role:**
  - `PUT /api/v1/admin/apps/{appId}/roles/{roleId}/permissions`
  - Body: `{ "permissionIds": ["guid-perm-1", "guid-perm-3"] }`

---

#### 4. Manajemen Pengguna (`Users`)

- **Daftar Pengguna (Paged & Search):**
  - `GET /api/v1/admin/users?search=&isActive=&page=1&pageSize=20`
  - Response: `PagedResponse<AdminUserDto>`
- **Detail Pengguna Lengkap:**
  - `GET /api/v1/admin/users/{id}`
  - Response: Detail akun + daftar aplikasi yang terhubung (`appAssignments`).
- **Buat Pengguna Baru:**
  - `POST /api/v1/admin/users`
  - Body:
    ```json
    {
      "username": "ahmad.dani",
      "email": "ahmad@askridasyariah.co.id",
      "password": "PasswordKuat123!",
      "fullName": "Ahmad Dani",
      "isActive": true,
      "isAccountVerified": true
    }
    ```
- **Update Pengguna:**
  - `PUT /api/v1/admin/users/{id}`
  - Body: `{ "email": "...", "fullName": "...", "isActive": true, "isAccountVerified": true }`
- **Buka Kunci Akun (Unlock User):**
  - `POST /api/v1/admin/users/{id}/unlock`
  - Response: `{ "message": "Kunci akun berhasil dibuka." }`
- **Cabut Seluruh Sesi Aktif User (Revoke Sessions):**
  - `POST /api/v1/admin/users/{id}/revoke-sessions`
  - Response: `{ "userId": "...", "revokedSessionsCount": 3, "message": "..." }`

---

#### 5. Penugasan Role Pengguna ke Suatu Aplikasi

- **Lihat Role User pada Aplikasi Tertentu:**
  - `GET /api/v1/admin/users/{userId}/apps/{appId}/roles`
- **Atur / Berikan Role User pada Aplikasi Tertentu:**
  - `PUT /api/v1/admin/users/{userId}/apps/{appId}/roles`
  - Body:
    ```json
    {
      "roleIds": ["guid-role-staff", "guid-role-manager"]
    }
    ```
  - Response: `UserAppRolesResponse` (daftar role dan daftar hak akses efektif `effectivePermissionCodes`).

---

#### 6. Audit Trail & Log Keamanan

- **Ambil Riwayat Audit Log (Paged & Filtered):**
  - `GET /api/v1/admin/audit-logs?clientId=&userId=&eventType=&isSuccess=&dateFromUtc=&dateToUtc=&page=1&pageSize=25`
  - Filter yang didukung:
    - `clientId`: Filter berdasarkan aplikasi (misal `auth-admin`).
    - `eventType`: Contoh `AUTH_LOGIN_SUCCESS`, `AUTH_LOGIN_FAILED`, `USER_LOCKED`.
    - `isSuccess`: `true` / `false`.
    - `page` dan `pageSize`.
  - Response: `PagedResponse<AuditLogDto>` (memuat detail event, IP address, user agent, dan `detailJson`).

---

### D. System & Monitoring

- **Health Check Status:**
  - `GET /health`
  - Response `200 OK`: `{ "status": "Healthy", "entries": { "self": { ... }, "mssql": { ... } } }`
- **Scalar OpenAPI UI (Khusus Environment Development):**
  - `GET /scalar/v1`

---

### E. Pegawai & Organisasi

#### Aturan bersama (admin)
- **Auth:** `Authorization: Bearer <accessToken>` dengan `aud = auth-admin`. Header `X-Api-Key` ditolak (403). Hak SuperAdmin diperiksa ulang ke database pada setiap panggilan (403 bila tidak punya).
- **Konkurensi:** semua mutasi atas resource yang sudah ada wajib `If-Match` berisi tepat satu ETag bertanda kutip (tanpa `W/`, tanpa koma) dari respons `GET` terakhir. Tanpa header → `428`; ETag usang → `412`; format salah → `400`.
- **Respons:** `Cache-Control: no-store`; ETag dikirim pada detail dan hasil mutasi.
- **Rate limit:** kebijakan `AdminEmployees`, 120 permintaan/60 detik (fixed window), lewat batas → `429`.
- **Kode error umum:** 400 validasi, 401, 403, 404, 409 (aturan bisnis/`InvalidOperation`), 412, 428, 429.
- Periode memakai tanggal bisnis Asia/Jakarta, awal inklusif, akhir eksklusif (`effectiveToExclusive`).

#### E1. Pegawai (`/api/v1/admin/employees…`)
| Endpoint | Keterangan | Body utama | Audit |
|---|---|---|---|
| `GET employees?search&page&pageSize` | Daftar paginasi (maks 100) | - | - |
| `GET employees/{id}` | Detail current + ETag | - | - |
| `GET employees/{id}/history` | Riwayat semua revisi | - | - |
| `POST employees` → 201 | Buat pegawai | `employeeNumber`, `employeeFullName`, `effectiveFrom`, `reason` | EmployeeCreated |
| `PUT employees/{id}` | Ubah identitas (If-Match) | `employeeNumber`, `employeeFullName`, `reason` | EmployeeIdentityUpdated |
| `POST employees/{id}/status-changes` | Ubah status ACTIVE/RESIGNED | `statusCode`, `effectiveFrom`, `reason`, `decisionReference?` | EmployeeStatusChanged |
| `POST employees/{id}/assignments` | Tambah penempatan | `positionId`, `isPrimary`, `effectiveFrom`, `effectiveToExclusive?`, `reason`, `decisionReference?` | EmployeePositionAssigned |
| `POST employees/{id}/transfers` | Mutasi/promosi | `sourceAssignmentId`, `targetPositionId`, `effectiveFrom`, `reason`, `decisionReference?` | EmployeePositionTransferred |
| `POST employees/{id}/assignments/{assignmentId}/revisions` | Akhiri penempatan (hanya `action: "END"`) | `action`, `effectiveToExclusive`, `reason` | EmployeePositionEnded |
| `POST employees/{id}/acting-assignments` | Tambah Plh/Plt | `sourceAssignmentId`, `targetPositionId`, `assignmentType`, `effectiveFrom`, `effectiveToExclusive?`, `decisionReference` (wajib), `reason` | EmployeeActingAssigned |
| `POST employees/{id}/acting-assignments/{actingId}/revisions` | Akhiri Plh/Plt (`END`) | sama dengan revisi penempatan | EmployeeActingEnded |

Catatan: `assignmentId`/`actingId` pada route adalah Id revisi row (berubah tiap revisi); ambil ulang detail setelah setiap mutasi. Plh/Plt tidak memberi permission otomatis.

#### E2. Tautan akun ↔ pegawai
- `GET /api/v1/admin/users/{userId}/organization` → `{ userId, employee | null }` (ETag hanya bila tertaut).
- `PUT /api/v1/admin/users/{userId}/employee-link` (If-Match) body `{ employeeId, reason }` — audit UserEmployeeLinked.
- `DELETE /api/v1/admin/users/{userId}/employee-link` (If-Match) body `{ reason }` — 404 bila tidak tertaut; audit UserEmployeeUnlinked. Body JSON wajib; pastikan proxy/BFF tidak membuangnya.

#### E3. Siklus hidup akun
- `POST /api/v1/admin/employees/lifecycle/process-resignations?dryRun=true` (default dry-run). Menonaktifkan akun aktif milik pegawai yang resign-nya sudah berlaku, mencabut refresh token, audit EmployeeAccountDeactivated; idempoten, batch 200 (`remaining`). Role/UserAppRoles tidak disentuh; reaktivasi pegawai tidak mengaktifkan akun lagi.
- Penegakan saat login/refresh/verify/permissions/me dikendalikan flag `SecuritySettings:EnforceEmploymentStatus` (default `false`; aktifkan hanya setelah data tervalidasi).

#### E4. Master organisasi (`/api/v1/admin/organization…`)
Baca: `GET classifications`; `GET offices?search&classification&asOf&page&pageSize`; `GET offices/{id}?includeHistory`; `GET offices/{id}/structure?asOf` (pemegang definitif & vacancy); `GET units?officeId&asOf`; `GET units/{id}`; `GET positions?officeId&unitId&asOf`; `GET positions/{id}`. Detail membawa ETag (= versi).

Tulis (201; audit `Organization{Office|Unit|Position}{Created|Versioned}`):
- `POST offices` — `officeCode` (3 digit), `name`, `classificationCode`, `effectiveFrom`, `reason`, `decisionReference?`.
- `POST units` — `officeId`, `unitCode`, `parentOrgUnitId?`, `unitType`, `name`, `effectiveFrom`, `reason`.
- `POST positions` — `orgUnitId`, `positionCode`, `name`, `track`, `isUnitHead`, `maxIncumbents?`, `levelId?`, `reportsToPositionId?`, `effectiveFrom`, `reason`.
- `POST {offices|units|positions}/{id}/versions` (If-Match) — field di atas ditambah `isActive`, tanpa kode. Versi yang berlaku di-supersede; ditolak bila ada versi lebih lanjut, tanggal mundur, atau lebih dari 5 tahun ke depan.
Setiap tulis memvalidasi ulang integritas seluruh timeline kantor dan penempatan pegawai terkait; gagal = rollback (409).

#### E5. Profil sendiri untuk aplikasi konsumen (`/api/v1/auth/me/organization`)
- **Auth:** Bearer JWT aplikasi konsumen (`[Authorize]`); pengguna dari `sub`, aplikasi dari `aud`; tidak menerima userId/asOf dari klien. Kebijakan rate limit `SelfProfile` 120/menit (per `sub`).
- `GET /api/v1/auth/me/organization` → `{ userId, accountDisplayName, asOf, businessTimeZone, profileVersion, nextEffectiveChangeAtUtc, employee | null, primaryAssignmentId, assignments[] }`. ETag = `profileVersion`; mendukung `If-None-Match` (termasuk `W/` dan `*`) → `304`. Akun tanpa tautan → 200 dengan `employee: null`. Pegawai RESIGNED → 403.
- `GET /api/v1/auth/me/reporting-line?assignmentId=` → `{ assignmentId, asOf, positions[] }`; atasan formal hanya posisi + vacancy (tanpa NIP/nama). `assignmentId` asing/tidak dikenal → 404 (tidak dapat dibedakan).
- Tidak ada field permission/approval; konsumen tetap bertanggung jawab atas scope dan approval. Lakukan polling berkala karena perubahan vacancy tidak tercermin di `nextEffectiveChangeAtUtc`.

---

## 4. Panduan Pengembangan Fitur Baru

Jika Anda atau agen AI lain ingin menambahkan fitur baru di backend:

1. **Buat / Perbarui Entitas Domain:**
   - Tambahkan entitas baru di `src/AuthApi.Domain/Entities/`.
   - Konfigurasikan relasi dan tabel pada `src/AuthApi.Infrastructure/Persistence/Configurations/`.
2. **Buat Use Case & DTO:**
   - Tambahkan request/response DTO di `src/AuthApi.Application/Features/.../DTOs/`.
   - Tambahkan logika bisnis di `I...Service` dan implementasinya.
3. **Ekspos via WebApi Controller:**
   - Tambahkan action method pada controller terkait di `src/AuthApi.WebApi/Controllers/`.
   - Gunakan return type `IActionResult` atau `ActionResult<T>` dengan status code eksplisit (`Ok`, `Created`, `NoContent`, `Problem`).
4. **Verifikasi & Pengujian:**
   - Jalankan `dotnet build AuthApi.sln`
   - Jalankan unit & integration test: `dotnet test AuthApi.sln`

