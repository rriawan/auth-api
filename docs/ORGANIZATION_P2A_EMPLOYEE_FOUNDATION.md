# P2A Pondasi Profil Pegawai dan Penempatan

Pengguna telah mengonfirmasi schema/master kantor 5/28/28 dan template draft 4/10/24 berhasil diterapkan. D-01 s.d. D-04 serta scope draft awal D-05 telah disetujui. Langkah ini menambahkan persistence profil pegawai dan aturan domain murni, tanpa membuat data pegawai atau mengubah login/RBAC.

## 1. Hasil implementasi

| Tabel | Fungsi |
|---|---|
| Employees | NIP string unik, nama resmi berbeda dari nama akun, rowversion agregat dan metadata pelaku |
| EmployeeStatuses | Riwayat ACTIVE/RESIGNED, periode efektif, status record, revisi dan alasan |
| EmployeePositions | Penempatan definitif utama/tambahan, posisi, periode dan revisi |
| EmployeeActingAssignments | PLH/PLT, sumber penempatan definitif, posisi target, referensi SK dan periode |
| UserEmployeeLinks | Kaitan current opsional 1:1 antara akun global dan pegawai |

Seluruh FK baru restrictive. Tidak ada unique PositionId yang membatasi Pelaksana menjadi satu orang. FK komposit pada sumber acting memastikan sumber definitif milik pegawai yang sama. Tanggal memakai SQL date, awal inklusif/akhir eksklusif. Tidak ada backfill atau data pegawai fiktif.

### Revisi dan draft

- Id adalah identitas row revisi; AssignmentKey adalah identitas logis satu penempatan/acting.
- Unique (AssignmentKey, RevisionNumber) menyimpan riwayat; filtered unique AssignmentKey dengan SupersededAtUtc null memastikan satu revisi current.
- DRAFT hanya digunakan untuk assignment baru pada baseline. Revisi DRAFT tidak boleh menyupersede CONFIRMED yang masih berlaku. Perubahan assignment confirmed harus dilakukan sebagai revisi confirmed secara atomik; workflow staging draft untuk perubahan existing ditunda.
- Acting merujuk Id revisi definitif yang menjadi dasarnya. P2B wajib menangani seluruh acting current yang merujuk sumber saat sumber diganti: referensi baru dan histori diperbarui atomik atau perubahan ditolak. Histori lama tetap menunjuk sumber lama.
- Status pegawai menggunakan nomor revisi per pegawai untuk seluruh timeline, bukan per jenis status. P2B memvalidasi tidak ada overlap/gap yang melanggar aturan timeline; schema sendiri tidak menjamin seluruhnya.
- Link user hanya menyimpan hubungan current. Histori hubungan memakai audit pada use-case selanjutnya, bukan snapshot identitas historis tersirat.

### Aturan domain yang tersedia

`EffectivePeriod` mendefinisikan Contains, Overlaps, Covers dan Intersect pada interval setengah terbuka, termasuk akhir null.

`PlacementScheduleRules` memeriksa jadwal yang diberikan pemanggil:

- Hanya revisi current CONFIRMED dihitung; DRAFT/CANCELLED/superseded diabaikan.
- Tidak ada dua jabatan utama yang overlap untuk satu pegawai; penempatan tambahan harus selalu tercakup jabatan utama, termasuk saat transisi mutasi.
- Kapasitas dievaluasi terhadap periode versi posisi yang berlaku, bukan angka kapasitas hari ini. Periode posisi nonaktif atau hilang tidak dianggap berkapasitas tak terbatas.
- Pergantian incumbent tepat pada batas akhir/awal hari yang sama tidak dianggap overlap.
- Sumber acting harus revisi definitif exact milik pegawai yang sama, current dan confirmed, serta mencakup seluruh periode acting. Penugasan acting wajib mempunyai referensi keputusan.

Aturan tersebut belum dipanggil dari API. Pemanggil P2B wajib memuat keseluruhan jadwal terkait, status pegawai serta struktur posisi efektif; memperoleh lock/transaksi yang konsisten; menjalankan validasi dan audit atomik; serta menaikkan rowversion Employee saat child berubah. Fungsi murni sendiri tidak mencegah race, tidak memverifikasi permission, dan tidak menentukan status kepegawaian/approver.

## 2. Penerapan migration oleh pengguna

Migration baru: `20261106055857_AddEmployeeProfileFoundation`. File dibuat menggunakan factory offline tanpa startup/seeder atau koneksi database. Periksa target dan konfigurasi Development sebelum menjalankan sendiri:

```powershell
dotnet ef database update 20261106055857_AddEmployeeProfileFoundation --project src/AuthApi.Infrastructure --startup-project src/AuthApi.WebApi -- --environment Development
```

Untuk environment lain, sesuaikan `--environment`. Jangan memakai `--offline-migrations` pada operasi database. Tidak perlu mengulangi provisioning kantor/template. Tidak ada script seed Employees, status pegawai, penempatan atau link akun pada langkah ini.

## 3. Verifikasi dan batas fase

Tes metadata memeriksa relasi, tipe tanggal, unique revision/link, rowversion, dan constraint. Tes domain memeriksa primary/capacity masa depan, coverage, batas hari, dan sumber acting. Tes migration memastikan Up hanya membuat lima tabel baru beserta index; tidak mengubah tabel auth/master existing.

Constraint dan race SQL Server riil tetap memerlukan pemeriksaan manual pengguna pada database pengujian terpisah. Hasil test metadata/domain tidak berarti server telah diuji. Jangan memasukkan penempatan langsung melalui SQL untuk melewati use-case yang belum selesai.

Setelah pengguna mengonfirmasi migration berhasil, lanjut P2B: use-case pencatatan profil/status/penempatan, link/unlink, mutasi dan acting dengan transaksi, audit, optimistic concurrency serta pemeriksaan seluruh periode. API/permission baru pada P3 mengikuti keputusan D-06; perilaku resign pada login/refresh/realtime dan proses penonaktifan akun baru dihubungkan pada P4 setelah gate operasional D-07. Antarmuka portal belum dibuat.
