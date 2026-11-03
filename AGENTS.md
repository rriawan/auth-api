# AGENTS.md — Instruksi Konteks & Aturan Projek `auth-api`

Dokumen ini menjadi memori permanen dan aturan utama bagi seluruh model AI yang bekerja di repository ini, baik saat **menyempurnakan perencanaan** maupun saat **mengeksekusi penulisan kode**.

## 1. Tujuan Repository
- Repository `auth-api` dikhususkan sebagai **layanan terpusat Authentication (AuthN) dan Authorization (AuthZ)** berbasis REST Web API.
- Setiap kali pemilik projek membuat projek/aplikasi baru di masa depan, aplikasi baru tersebut cukup memanggil endpoint API dari repo ini (atau memverifikasi token JWT via endpoint JWKS repo ini) tanpa perlu membangun sistem autentikasi/otorisasi sendiri.
- Dokumen blueprint utama tersimpan di `PLANNING.md` dan catatan progres pengerjaan tersimpan di `STATUS.md` pada root repository. **Selalu baca `STATUS.md` dan `PLANNING.md` terlebih dahulu sebelum mengubah perencanaan atau menulis kode.**

## 2. Spesifikasi Teknologi Mutlak (Hard Constraints)
1. **Versi .NET:** Wajib menggunakan **.NET Core / .NET versi Long Term Support (LTS) terbaru dengan versi patch tertinggi**, yaitu **.NET 10 LTS (`net10.0`, lini `10.0.x` tertinggi)**.
   - **PERINGATAN UNTUK MODEL AI:** Jangan pernah menurunkan target framework ke `.NET 8` atau `.NET 9` meskipun pengetahuan bawaan (*training cutoff*) Anda lebih lama, dan jangan menggunakan versi STS (non-LTS seperti `.NET 11`). Seluruh `.csproj` wajib memakai `<TargetFramework>net10.0</TargetFramework>` dan kunci SDK major `10` di `global.json` (`rollForward: latestFeature`).
2. **ORM:** Wajib menggunakan **Entity Framework Core 10** (`Microsoft.EntityFrameworkCore.SqlServer` versi stabil `10.0.*` tertinggi) dengan pendekatan **Code-First Migrations**.
3. **Database:** Wajib menggunakan **Microsoft SQL Server (MSSQL)**.
4. **OpenAPI UI:** Gunakan `Microsoft.AspNetCore.OpenApi` (`10.0.*`) bawaan .NET 10 yang dipadukan dengan `Scalar.AspNetCore` (hindari `Swashbuckle.AspNetCore` lama kecuali diminta eksplisit oleh user).

## 3. Keputusan Arsitektur & Bisnis yang Telah Dikunci
Seluruh perencanaan lanjutan maupun implementasi kode wajib konsisten dengan keputusan pemilik projek berikut:
- **Model Akun User:** **Single User Pool (SSO Terpusat)** — 1 akun user global dapat dipakai lintas projek/aplikasi, namun hak akses diisolasi per aplikasi terdaftar (`ClientApp`).
- **Pendekatan Protokol API:** **RESTful JSON Auth API + JWT Asymmetric (RS256 + JWKS endpoint `/.well-known/jwks.json`) + Refresh Token Rotation + Endpoint Verifikasi Realtime (`/api/v1/auth/verify` & `/api/v1/auth/permissions`)**. Jangan membangun server protokol OAuth2/OIDC penuh (seperti OpenIddict/IdentityServer) agar integrasi projek baru tetap ringan dan langsung siap pakai.
- **Granularitas Authorization:** **Role + Permission per Aplikasi** — User ditugaskan ke `Role` pada suatu `ClientApp`, dan setiap `Role` memiliki daftar `Permission` spesifik pada `ClientApp` tersebut (contoh: `report.view`, `user.create`).
- **Fitur Keamanan & Pendukung yang Masuk Scope:**
  1. **Client ID & API Key Management** untuk mendaftarkan projek/aplikasi baru dan mengautentikasi komunikasi antar-backend (`X-Client-Id` + `X-Api-Key`).
  2. **Audit Log & Account Lockout** untuk pencatatan riwayat keamanan login/auth dan proteksi *brute-force* (ditambah Rate Limiting).
  3. **Alur Aktivasi Akun & Forgot/Reset Password** berbasis *one-time hashed token* yang dirancang *decoupled* (melalui interface abstraksi pengirim notifikasi) agar siap dihubungkan ke layanan Email/WhatsApp kapan saja.
- **Out of Scope Saat Ini (Jangan Dibuat Tanpa Instruksi Baru):**
  - Two-Factor Authentication (2FA / OTP).
  - Full OAuth2 / OIDC flows.
  - Aplikasi Frontend / UI Admin Dashboard.

## 4. Aturan Saat Diminta Mengubah atau Menambah Dokumen Planning
- Pertahankan gaya penulisan `PLANNING.md` pada level **High-Level** (fokus pada arsitektur, alur kerja, kontrak endpoint, desain konseptual database, aturan keamanan, dan tahapan pengerjaan).
- **Hindari** menaruh potongan kode C# implementasi panjang di dalam dokumen planning.
- Tuliskan instruksi secara eksplisit, terstruktur, dan bebas ambiguitas karena `PLANNING.md` dirancang khusus agar dapat dieksekusi dengan aman oleh **Junior Programmer** atau **Model AI yang lebih murah/kecil**.

## 5. Aturan Saat Diminta Mengimplementasikan Kode (Untuk Model Eksekutor)
1. **Kerjakan Bertahap per Fase:** Ikuti urutan **Fase 1 s/d Fase 8** di `PLANNING.md` satu per satu. Jangan menulis semua fase sekaligus dalam satu langkah besar.
2. **Patuhi Struktur 4 Project (Clean/Layered Architecture):**
   - `AuthApi.Domain` (Entitas & Enum murni tanpa dependensi eksternal)
   - `AuthApi.Application` (DTO, Interface, Validasi, Use-Case Bisnis)
   - `AuthApi.Infrastructure` (EF Core 10 MSSQL, Migrations, RSA/JWT RS256, Hashing, Audit Log)
   - `AuthApi.WebApi` (Tipis/Thin Endpoints, Middleware, Rate Limit, OpenAPI, DI)
3. **Wajib Aman secara Kriptografi:**
   - Dilarang menyimpan Password, Refresh Token, Client API Key, atau Token Reset/Aktivasi secara *plain-text* di database (hash password dengan PasswordHasher/Argon2id; hash token & API key dengan SHA-256).
   - Dilarang melakukan hardcode atau commit kredensial MSSQL maupun RSA Private Key ke Git.
4. **Verifikasi Setiap Selesai Satu Langkah:** Selalu jalankan `dotnet build` (dan `dotnet test` bila project test sudah tersedia) untuk memastikan tidak ada *compile error* sebelum melaporkan sebuah fase selesai.
5. **Perbarui `STATUS.md` Secara Berkala:** Setiap kali selesai mengerjakan satu fase atau langkah penting, selalu perbarui file `STATUS.md` agar status terakhir selalu siap dilanjutkan kapan saja.
6. **Tanya Jika Ragu:** Jika menemukan kebutuhan yang bertentangan dengan `PLANNING.md` atau belum jelas, tanyakan konfirmasi ke user terlebih dahulu sebelum berasumsi.

## 6. Aturan Git & Version Control
1. **Jangan Commit Tanpa Instruksi Eksplisit:** Dilarang melakukan `git commit` (ke repository lokal) maupun `git push` (ke GitHub/remote) apabila belum diminta secara eksplisit oleh user.
2. **Tanpa Atribusi AI pada Commit:** Saat diminta melakukan commit ke Git/GitHub, **dilarang keras** menambahkan atribusi pembuat atau co-author di pesan commit maupun metadata git (seperti `Co-authored-by:`, `Created by`, `Generated with ...`, atau atribusi AI/model lainnya). Gunakan pesan commit yang bersih murni berisi deskripsi perubahan.

## 7. Aturan Operasi Database (Dikerjakan Manual oleh User)
1. **Dilarang Menjalankan Operasi Database Apa Pun:** Model AI **tidak boleh** mengeksekusi perintah atau tindakan apa pun yang terkait database, termasuk namun tidak terbatas pada:
   - Menerapkan atau membatalkan migration ke database (`dotnet ef database update`, `dotnet ef database drop`, `Database.Migrate()` otomatis saat startup, dan sejenisnya).
   - Menghapus migration lewat `dotnet ef migrations remove`, karena perintah ini memeriksa status migration langsung ke database. Jika perlu, berikan sebagai instruksi untuk user.
   - Menjalankan script SQL / query (`sqlcmd`, `Invoke-Sqlcmd`, SSMS, dsb.) untuk `CREATE`, `ALTER`, `DROP`, `INSERT`, `UPDATE`, `DELETE`, `TRUNCATE`, seeding data, maupun backup/restore.
   - Membuat, menghapus, atau mengubah database, tabel, user/login, maupun connection string pada server MSSQL.
2. **Yang Tetap Boleh Dikerjakan AI:**
   - Menulis/mengubah kode C# terkait persistence (entitas Domain, `DbContext`, konfigurasi Fluent API `IEntityTypeConfiguration`, repository).
   - Membuat file migration pada kode (`dotnet ef migrations add`), selama hanya menghasilkan file migration di dalam project dan **tidak** menerapkannya ke database.
3. **Berikan Instruksi ke User:** Apabila suatu langkah membutuhkan operasi langsung ke database (misalnya menerapkan migration dengan `dotnet ef database update`), AI cukup **menuliskan instruksi langkah demi langkah beserta perintah yang perlu dijalankan** (contoh lengkap dengan parameter `--project` dan `--startup-project`), lalu **menunggu user** mengeksekusinya sendiri dan mengonfirmasi hasilnya sebelum melanjutkan.

## 8. Aturan Memori Kerja & Kontinuitas Sesi (`STATUS.md`)
1. **Wajib Periksa Sebelum Bekerja:** Setiap kali sesi dimulai atau dilanjutkan, model AI **wajib** membaca `STATUS.md` pada root repository terlebih dahulu untuk mengetahui posisi pekerjaan terakhir.
2. **Struktur Wajib `STATUS.md`:** File `STATUS.md` harus selalu memiliki dan memelihara 4 bagian utama berikut:
   - **Sedang Dikerjakan:** Posisi fase/tugas aktif saat ini beserta daftar centang (*checklist*) progres Fase 1 s/d Fase 8.
   - **Keputusan Penting:** Keputusan teknis/implementasi penting yang telah diambil selama pengerjaan.
   - **Masalah yang Diketahui:** Daftar kendala terbuka, catatan teknis, atau langkah manual yang masih menunggu eksekusi.
   - **Langkah Berikutnya:** Urutan tindakan konkret selanjutnya agar sesi baru dapat langsung mengeksekusi pekerjaan tanpa mengulang analisis dari awal.
3. **Wajib Perbarui Setelah Bekerja:** Setiap kali menyelesaikan suatu fase, membuat keputusan teknis baru, menemukan masalah, atau sebelum mengakhiri giliran kerja penting, model AI **wajib** memperbarui isi `STATUS.md`.


