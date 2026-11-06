# P1B Template Operasional Draft

Pengguna telah mengonfirmasi provisioning kantor 5/28/28 serta menyetujui empat template operasional dalam status DRAFT. Langkah ini mengisi definisi template saja. Tidak membuat unit/posisi kantor aktual, tidak menerapkan template ke 28 kantor, tidak mengubah penempatan pegawai atau RBAC, dan tidak mempublikasikan template.

## 1. Katalog dan batas data

Sumber: `data/organization/operational-templates.v1.json`, berdasarkan pola cabang/pemasar pada BRD dan blueprint. Kode unit/posisi adalah kode teknis usulan. Root unit OTHER merupakan kontainer teknis template; bukan klaim master unit baru perusahaan. Unit SECTION mengikuti seksi yang disebut dalam sumber.

| Klasifikasi | Unit template | Posisi template |
|---|---:|---:|
| CAB-I | 4 | 8 |
| CAB-II | 3 | 7 |
| CAB-III | 2 | 6 |
| PEMASAR | 1 | 3 |
| Total | 10 | 24 |

Kepala kantor dan kepala seksi ditandai IsUnitHead=true, kapasitas definitif 1. Pelaksana berkapasitas null (multipegawai). Semua jalur LINE. Seluruh LevelId masih null; pemetaan level, terutama Kepala Cabang/Kepala Pemasar, belum diputuskan. Fungsional dan struktur pusat tidak dibuat dari asumsi. Draft tidak boleh dipublikasikan atau diterapkan sebelum master yang diperlukan tersedia dan validasi publikasi/use-case selesai.

## 2. Pembuatan file SQL secara offline

Perintah berikut hanya membaca katalog lokal dan menulis ulang file SQL; tidak memanggil SQL Server atau menjalankan script:

```powershell
powershell -NoProfile -ExecutionPolicy RemoteSigned -File scripts/New-OperationalTemplateProvisioning.ps1
```

Output: `scripts/database/provision-organization-templates.sql`. Opsi RemoteSigned hanya berlaku pada proses generator tersebut, tidak mengubah kebijakan PowerShell permanen. File hasil generator disertakan agar dapat ditinjau sebelum eksekusi manual. Perubahan katalog harus diikuti regenerasi, pengujian bisnis/topologi, dan review script. Jangan mengedit data baseline langsung pada SQL hasil generator.

## 3. Eksekusi manual oleh pengguna

Tidak perlu migration baru untuk langkah ini karena tabel template sudah ada pada P1A. Gunakan kode backend terbaru saat menampilkan audit log agar event `OrganizationTemplatesProvisioned` dikenali. Menjalankan/restart aplikasi yang memakai database tetap dilakukan pengguna.

1. Pastikan target server/database sama dengan yang sudah berhasil diisi master kantor. Tinjau file SQL terbaru.
2. Gunakan GUID SuperAdmin aktif dan terverifikasi yang sama seperti provisioning kantor. Tidak perlu tanggal efektif karena langkah ini hanya membuat draft definisi; CreatedAtUtc adalah waktu pencatatan, bukan tanggal berlaku struktur kantor.
3. Jalankan sendiri dari root repository. Contoh SQL Authentication, password diminta interaktif:

```powershell
sqlcmd -S "<server>" -d "<database>" -U "<login-sql>" -b -i "scripts/database/provision-organization-templates.sql" -v ActorUserId="<GUID-SuperAdmin>"
```

Gunakan `-E` sebagai pengganti `-U` hanya jika Windows Authentication memang sesuai akun/domain yang dipercaya server. Jangan mengirim password ke AI.

4. Pada provisioning pertama yang belum memiliki template baseline, hasil diharapkan:

```text
InsertedTemplates InsertedUnits InsertedPositions
4                 10            24
```

5. Eksekusi ulang dengan parameter sama tidak menggandakan template: hasil 0/0/0, audit keberhasilan tetap dicatat. Jika baseline sudah berbeda, tidak lengkap, atau versi lain ada tanpa versi 1, script menolak dan rollback; jangan menghapus atau menimpa data untuk memaksa hasil hitungan.
6. Kirim hasil eksekusi kepada AI. Tidak ada operasi database yang dijalankan AI.

Script memakai transaksi, validasi actor, penguncian dan pemeriksaan graph dua arah. Existing template yang cocok tidak diubah, termasuk jika sudah published. Script hanya menambahkan agregat baru dalam keadaan DRAFT; tidak mengubah publication metadata atau menyalin data ke Offices/OrgUnits/Positions. JSON audit dibangun dari nilai bertipe tanpa FOR JSON/WITHOUT_ARRAY_WRAPPER.

## 4. Verifikasi dan kelanjutan

Uji katalog memeriksa garis pelaporan yang membedakan CAB-I/II/III dan PEMASAR, referensi internal, ketiadaan cycle, status draft, level yang belum dipetakan, serta kapasitas sesuai D-04. Parser lokal memeriksa sintaks SQL tanpa koneksi. Kedua hal tersebut tidak membuktikan constraint/locking di server; eksekusi dan pemeriksaan riil tetap oleh pengguna.

Setelah provisioning draft dikonfirmasi, P2 dapat menambah profil pegawai dan penempatan berdasarkan keputusan D-01/D-02/D-03/D-04. Publish/apply template dan pengisian struktur pusat menunggu kelengkapan master D-05. Hak akses data, SLA/lifecycle operasional dan retensi menunggu gate D-06 s.d. D-08.
