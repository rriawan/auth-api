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
| Subtotal operasional | 10 | 24 |
| HEAD_OFFICE (Kantor Pusat) | 89 | 89 |
| Total | 99 | 113 |

Template HEAD_OFFICE (ditambahkan setelah data struktur pusat dari pengguna) hanya memuat jabatan pimpinan unit: 3 Direktorat (Direktur Utama/Operasional/Pemasaran, tanpa atasan), 11 Divisi, 24 Bagian, 51 Seksi. Setiap unit memiliki tepat satu jabatan pimpinan berkapasitas 1 yang melapor ke pimpinan unit induknya. Aktuaris Perusahaan dan Sekretaris Perusahaan bertipe Divisi; "Unit Kerja Data Nasabah" bertipe Seksi dengan jabatan "Kepala Unit Kerja Data Nasabah". Kode memakai penomoran teknis usulan (`DIR_01`, `DIV_01`, `BAG_01`, `SEK_01`; jabatan `HEAD_<kode unit>`). Jabatan staf/pelaksana pusat belum dimodelkan.

Kepala kantor dan kepala seksi ditandai IsUnitHead=true, kapasitas definitif 1. Pelaksana berkapasitas null (multipegawai). Semua jalur LINE. Master level jabatan (5 level: DIREKTUR, KADIV, KABAG, KASI, PELAKSANA) disetujui pengguna dan ikut dibuat oleh script ini; 89 jabatan HEAD_OFFICE dipetakan menurut tipe unit (Direktorat→DIREKTUR, Divisi→KADIV, Bagian→KABAG, Seksi→KASI). PELAKSANA belum dipakai jabatan mana pun. Template operasional (CAB-I/II/III, PEMASAR) tetap tanpa level (LevelId null) karena strukturnya dinyatakan terpisah oleh pengguna. Fungsional dan struktur pusat tidak dibuat dari asumsi. Draft tidak boleh dipublikasikan atau diterapkan sebelum master yang diperlukan tersedia dan validasi publikasi/use-case selesai.

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
InsertedLevels InsertedTemplates InsertedUnits InsertedPositions
5              5                 99            113
```

   Jika empat template operasional (4/10/24) sudah pernah diprovisioning (kondisi Anda saat ini), template lama hanya dibandingkan (tidak diubah) dan hasil pertama setelah penambahan struktur pusat dan level adalah **5/1/89/89**. Error baru: 51106 (level dengan kode sama sudah ada tetapi nama/urutan berbeda) dan 51107 (kode level pada jabatan tidak ditemukan); keduanya rollback tanpa menulis data.

5. Eksekusi ulang dengan parameter sama tidak menggandakan template: hasil 0/0/0/0, audit keberhasilan tetap dicatat. Jika baseline sudah berbeda, tidak lengkap, atau versi lain ada tanpa versi 1, script menolak dan rollback; jangan menghapus atau menimpa data untuk memaksa hasil hitungan.
6. Kirim hasil eksekusi kepada AI. Tidak ada operasi database yang dijalankan AI.

Script memakai transaksi, validasi actor, penguncian dan pemeriksaan graph dua arah. Existing template yang cocok tidak diubah, termasuk jika sudah published. Script hanya menambahkan agregat baru dalam keadaan DRAFT; tidak mengubah publication metadata atau menyalin data ke Offices/OrgUnits/Positions. JSON audit dibangun dari nilai bertipe tanpa FOR JSON/WITHOUT_ARRAY_WRAPPER.

## 3b. Template versi 2 (level jabatan dan Pelaksana pusat)

Template DRAFT versi 1 yang sudah terpasang tidak diubah. Perubahan berikutnya dibuat sebagai **versi 2** yang berdampingan (unik per klasifikasi+versi):

- Katalog `data/organization/operational-templates.v2.json`: struktur Cabang/Pemasar sama dengan v1; Kepala Seksi dipetakan ke KASI, Pelaksana ke PELAKSANA; Kepala Cabang/Kepala Pemasar **tanpa level** karena setingkat Kadiv/Kabag/Kasi tergantung pemangku. HEAD_OFFICE ditambah 51 jabatan Pelaksana (satu per seksi, melapor ke Kepala Seksi, kapasitas banyak, nama mengikuti nama seksi). Total 99 unit, 164 jabatan.
- Generate (file saja): `powershell -NoProfile -ExecutionPolicy RemoteSigned -File scripts/New-OperationalTemplateProvisioning.ps1 -Version 2` menghasilkan `scripts/database/provision-organization-templates-v2.sql`.
- Jalankan manual setelah v1 (urutan wajib v1 lalu v2; v2 tidak mengubah v1), parameter sama:

```powershell
sqlcmd -S "<server>" -d "<database>" -U "<login-sql>" -b -i "scripts/database/provision-organization-templates-v2.sql" -v ActorUserId="<GUID-SuperAdmin>"
```

Hasil pertama yang diharapkan: `InsertedLevels=0, InsertedTemplates=5, InsertedUnits=99, InsertedPositions=164`; eksekusi ulang 0/0/0/0. Error 51103 berarti ada versi template lebih tinggi dari 2; 51105 berarti v2 yang ada di DB berbeda dari katalog.

## 4. Verifikasi dan kelanjutan

Uji katalog memeriksa garis pelaporan yang membedakan CAB-I/II/III dan PEMASAR, referensi internal, ketiadaan cycle, status draft, pemetaan level (hanya HEAD_OFFICE), serta kapasitas sesuai D-04. Parser lokal memeriksa sintaks SQL tanpa koneksi. Kedua hal tersebut tidak membuktikan constraint/locking di server; eksekusi dan pemeriksaan riil tetap oleh pengguna.

Setelah provisioning draft dikonfirmasi, P2 dapat menambah profil pegawai dan penempatan berdasarkan keputusan D-01/D-02/D-03/D-04. Publish/apply template dan pengisian struktur pusat menunggu kelengkapan master D-05. Hak akses data, SLA/lifecycle operasional dan retensi menunggu gate D-06 s.d. D-08.

## 4. Pilot: publikasi dan penerapan template ke kantor (dijalankan pengguna lewat portal)

Tidak ada langkah database manual; semua lewat portal sebagai SuperAdmin `auth-admin`. AI tidak menjalankan langkah ini.

1. Buka `/organization/offices/{id}` untuk kantor 110 (CAB-II). Panel "Template struktur" tampil hanya bila kantor belum memiliki unit/jabatan.
2. Pada template **versi 2** klasifikasi CAB-II (status Draft), klik **Publikasikan**, isi alasan/referensi keputusan. Publikasi final dan tidak dapat diubah.
3. Klik **Pratinjau & terapkan**, pilih tanggal efektif (hari ini atau sesudahnya), klik **Pratinjau**, periksa pohon unit/jabatan dan blocker, isi alasan, klik **Terapkan**. Hasil: jumlah unit dan jabatan yang dibuat.
4. Ulangi untuk kantor 119 (PEMASAR) dengan template PEMASAR versi 2.
5. Jangan memilih template versi 1 (tanpa level untuk KASI/PELAKSANA). Belum ada pembatalan penerapan; bila salah, laporkan agar dibuat use-case koreksi (jangan mengubah database langsung).
6. Uji di database pengujian dulu bila tersedia: apply memakai transaksi Serializable dan applock yang belum diuji di SQL Server riil oleh AI.
