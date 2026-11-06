# P1A Master Organisasi dan Langkah Database Manual

Dokumen ini mendampingi implementasi awal `PLANNING_ORGANIZATION_EMPLOYEE_PROFILE.md`. P1A hanya menambahkan persistence master organisasi. Belum ada API/halaman organisasi, data pegawai, status resign, penempatan, atau perubahan role. Fase P1 keseluruhan belum selesai.

## 1. Isi perubahan

- Dua belas tabel baru: OfficeClassifications, Offices, OfficeVersions, OrgUnits, OrgUnitVersions, PositionLevels, Positions, PositionVersions, StructureTemplates, TemplateOrgUnits, TemplatePositions, OfficeTemplateApplications.
- Versi memakai SQL `date`, awal inklusif dan akhir eksklusif. Versi sebelumnya dipertahankan dengan metadata superseded; validasi overlap/cycle lintas rentang memerlukan use-case pada fase lanjutan.
- RowVersion dan constraint/index melindungi integritas dasar. Tidak ada unique global PositionId untuk jumlah pemegang, dan belum ada asumsi kapasitas pimpinan.
- Script provisioning berisi 28 kantor resmi dan lima klasifikasi, tanpa mengisi struktur pusat, level jabatan, unit, posisi, atau template yang belum dipublikasikan melalui use-case.
- Tidak ada seed otomatis, perubahan login/JWT/RBAC, atau penerapan SQL oleh AI.

## 2. Pembuatan migration secara offline

Factory design-time berada di project startup WebApi dan tidak menjalankan Program.cs, memuat RSA, memanggil health check, atau menjalankan seeder. Mode offline juga tidak membaca konfigurasi aplikasi; mode tersebut memakai connection string placeholder dan tidak membuka koneksi saat membuat migration.

```powershell
dotnet ef migrations add AddOrganizationMaster --project src/AuthApi.Infrastructure --startup-project src/AuthApi.WebApi -- --offline-migrations
```

Perintah di atas hanya untuk menghasilkan file; jangan mengulanginya bila migration sudah ada. Jangan menggunakan `migrations remove` untuk membatalkan berkas karena dapat memeriksa database. Mode `--offline-migrations` tidak boleh dipakai pada perintah database.

## 3. Penerapan oleh pengguna

Seluruh tindakan pada bagian ini hanya dilakukan pengguna/operator. Pastikan target lingkungan, hak akses, backup dan rencana pemulihan sudah sesuai prosedur perusahaan. Peninjauan file migration harus memastikan tabel auth existing tidak dihapus atau diubah.

1. Pastikan `ConnectionStrings:DefaultConnection` tersedia pada konfigurasi lokal yang biasa dipakai WebApi. Factory membaca `src/AuthApi.WebApi/appsettings.json`, lalu `appsettings.{Environment}.json`, user-secrets WebApi khusus Development, dan terakhir environment variables. `ConnectionStrings__DefaultConnection` tetap didukung dan mempunyai prioritas tertinggi, tetapi tidak lagi wajib bila sumber lain sudah berisi koneksi. Jangan mengirim nilai koneksi kepada AI, menuliskannya dalam Git, atau mencetaknya ke log.
2. Terapkan migration dengan perintah berikut, tanpa flag offline:

```powershell
dotnet ef database update 20261106043118_AddOrganizationMaster --project src/AuthApi.Infrastructure --startup-project src/AuthApi.WebApi
```

Environment dipilih dari argumen eksplisit `--environment`, lalu `DOTNET_ENVIRONMENT`, lalu `ASPNETCORE_ENVIRONMENT`; default tooling lokal adalah `Development`. Jangan mengandalkan default Development untuk target production. Operator dapat memilih environment eksplisit, misalnya:

```powershell
dotnet ef database update 20261106043118_AddOrganizationMaster --project src/AuthApi.Infrastructure --startup-project src/AuthApi.WebApi -- --environment Production
```

Factory menemukan folder WebApi dari root repository atau folder project. Untuk lokasi kerja lain, tambahkan `-- --content-root "<direktori-AuthApi.WebApi>" --environment "<environment>"`. Argumen setelah `--` diteruskan ke factory; jangan gunakan flag offline saat menerapkan migration. Peringatan EF Tools `10.0.12` dibanding runtime `10.0.14` terpisah dari kegagalan konfigurasi koneksi; tidak diperlukan migration baru untuk perbaikan factory.

3. Tentukan tanggal awal master yang benar-benar disahkan perusahaan. Tanggal ini wajib diberikan eksplisit; script tidak menyimpulkan tanggal pembukaan kantor atau menggunakan tanggal deployment. Master mencerminkan daftar yang diketahui saat penyusunan blueprint, bukan rekonstruksi masa lalu. Pilih pula GUID user SuperAdmin internal yang aktif dan terverifikasi sebagai pelaku provisioning.
4. Tinjau `scripts/database/provision-organization-offices.sql`. Jalankan sendiri dengan SQLCMD, ganti seluruh placeholder dan gunakan metode autentikasi database yang disetujui. Contoh berikut menggunakan Windows Integrated Authentication; tidak berisi password:

```powershell
sqlcmd -S "<server>" -d "<database>" -E -b -i "scripts/database/provision-organization-offices.sql" -v EffectiveFrom="yyyy-MM-dd" ActorUserId="<GUID-SuperAdmin>"
```

Jika koneksi aplikasi memakai SQL Server Authentication (`User ID`/password), gunakan login SQL yang sama sesuai izin operator, bukan `-E`:

```powershell
sqlcmd -S "<server>" -d "<database>" -U "<login-sql>" -b -i "scripts/database/provision-organization-offices.sql" -v EffectiveFrom="yyyy-MM-dd" ActorUserId="<GUID-SuperAdmin>"
```

Tanpa `-P`, sqlcmd meminta password secara interaktif jika tidak tersedia melalui environment. Login SQL adalah identitas koneksi database, berbeda dari akun SuperAdmin portal pada ActorUserId. Error `untrusted domain ... Windows authentication` berasal dari jalur `-E`; tidak membuktikan script atau migration gagal. Bila server memang hanya menerima Windows Authentication, gunakan sesi Windows dari domain yang dipercaya sesuai arahan administrator database; jangan mengubah login atau konfigurasi server sebagai bagian troubleshooting AI.

Jika percobaan dengan script versi awal menghasilkan error pada `WITHOUT_ARRAY_WRAPPER` dan error lanjutan `@DetailJson`, gunakan file lokal yang sudah diperbarui dan ulangi perintah provisioning dengan parameter yang sama. Script kini menyusun objek audit dari GUID/tanggal/integer tervalidasi tanpa `FOR JSON` atau `WITHOUT_ARRAY_WRAPPER`. Sintaks file hasil perbaikan telah diperiksa offline dengan parser T-SQL 110, tanpa koneksi atau eksekusi SQL; kompatibilitas dan hasil di server tetap menunggu eksekusi pengguna. Tidak perlu membuat atau menerapkan ulang migration untuk koreksi script ini.

5. Hasil pertama pada database baru: InsertedClassifications=5, InsertedOffices=28, InsertedVersions=28. Script melakukan seluruh perubahan dan audit dalam satu transaksi; gagal berarti rollback. Jika ada master yang sudah berbeda, script menolak dan meminta review, bukan overwrite.
6. Ulangi dengan parameter yang sama untuk menguji idempotensi: jumlah insert master harus 0/0/0. Audit mencatat setiap eksekusi yang berhasil, termasuk retry tanpa perubahan master.
7. Konfirmasikan hasil migration/provisioning kepada AI sebelum melanjutkan pekerjaan yang memerlukan schema tersebut. Jangan kirim connection string atau informasi pribadi.

Perbedaan nama resmi, klasifikasi, maupun tanggal baseline memerlukan review manual. Retry tidak menyetel ulang histori yang sudah berubah secara sah. Tidak ada insert/update Roles, Permissions, RolePermissions, Users, atau UserAppRoles dalam script; tabel tersebut hanya dibaca untuk memvalidasi pelaku. AuditLogs mendapat satu record per eksekusi sukses.

## 4. Pemeriksaan SQL Server oleh pengguna

Unit test metadata tidak menjalankan SQL Server dan tidak membuktikan constraint/concurrency di server. Pada database pengujian terpisah, pengguna perlu memverifikasi:

- Kode `000` tersimpan sebagai string dan nama `PEMASAR SERANG` tidak berubah.
- Kode kantor duplikat atau bukan tepat tiga digit ditolak.
- Interval dengan akhir <= awal, kapasitas <= 0, dan parent diri sendiri ditolak.
- FK dan unique index sesuai model; penghapusan master yang dirujuk ditolak.
- RowVersion berubah ketika record identity di-update; implementasi use-case berikutnya wajib menaikkan versi agregat saat child berubah.
- Script yang dijalankan bersamaan tidak membuat kantor/versi awal ganda; perbedaan parameter ditolak bila tidak sesuai baseline.
- Kesalahan saat menulis audit menyebabkan provisioning rollback.
- Tabel, login, dan role existing tetap berfungsi.

Jangan menjalankan pemeriksaan di atas pada production dengan data percobaan. Validasi cycle multi-node, kapasitas penempatan, overlap periode, lifecycle akun, dan permission API baru belum termasuk perilaku P1A karena use-case tersebut belum dibuat.

## 5. Langkah berikutnya

Konfirmasi keputusan D-02 dan D-04 s.d. D-08 sesuai fase. Sebelum mengisi struktur pusat, level, hubungan antarkantor atau menerapkan template, diperlukan master dan keputusan yang sah. P1A tidak menerjemahkan informasi jabatan menjadi permission dan tidak mengubah RBAC existing.
