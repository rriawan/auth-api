-- GENERATED from data/organization/operational-templates.v1.json.
-- Regenerate with scripts/New-OperationalTemplateProvisioning.ps1; review before use.
-- Operator-run only. SQLCMD variable: ActorUserId (active system SuperAdmin GUID).
-- Creates position levels and DRAFT template definitions only; never publishes or applies them to offices.
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;

DECLARE @ActorUserId uniqueidentifier = TRY_CONVERT(uniqueidentifier, '$(ActorUserId)');
IF @ActorUserId IS NULL THROW 51100, 'Provide valid ActorUserId.', 1;

DECLARE @Levels TABLE (LevelCode nvarchar(50) PRIMARY KEY, Name nvarchar(150), DisplayOrder int);
DECLARE @Templates TABLE (ClassificationCode nvarchar(20) PRIMARY KEY, TemplateId uniqueidentifier, IsNew bit);
DECLARE @Units TABLE (
    ClassificationCode nvarchar(20), UnitCode nvarchar(50), ParentCode nvarchar(50),
    Name nvarchar(200), UnitType varchar(30), UnitId uniqueidentifier,
    PRIMARY KEY (ClassificationCode, UnitCode)
);
DECLARE @Positions TABLE (
    ClassificationCode nvarchar(20), PositionCode nvarchar(50), UnitCode nvarchar(50),
    SupervisorCode nvarchar(50), Name nvarchar(200), Track varchar(20),
    IsUnitHead bit, MaxIncumbents int, LevelCode nvarchar(50), PositionId uniqueidentifier,
    PRIMARY KEY (ClassificationCode, PositionCode)
);
INSERT INTO @Levels (LevelCode, Name, DisplayOrder) VALUES
(N'DIREKTUR', N'Direktur', 1),
(N'KADIV', N'Kepala Divisi / Setingkat', 2),
(N'KABAG', N'Kepala Bagian', 3),
(N'KASI', N'Kepala Seksi', 4),
(N'PELAKSANA', N'Pelaksana', 5);
INSERT INTO @Units (ClassificationCode, UnitCode, ParentCode, Name, UnitType) VALUES
(N'CAB-I', N'ROOT', NULL, N'Kantor Cabang', N'OTHER'),
(N'CAB-I', N'MARKETING', N'ROOT', N'Seksi Pemasaran', N'SECTION'),
(N'CAB-I', N'TECHNICAL', N'ROOT', N'Seksi Teknik', N'SECTION'),
(N'CAB-I', N'FINANCE', N'ROOT', N'Seksi Keuangan, SDM, dan Umum', N'SECTION'),
(N'CAB-II', N'ROOT', NULL, N'Kantor Cabang', N'OTHER'),
(N'CAB-II', N'MARKETING', N'ROOT', N'Seksi Pemasaran', N'SECTION'),
(N'CAB-II', N'OPERATIONS', N'ROOT', N'Seksi Operasional', N'SECTION'),
(N'CAB-III', N'ROOT', NULL, N'Kantor Cabang', N'OTHER'),
(N'CAB-III', N'OPERATIONS', N'ROOT', N'Seksi Operasional', N'SECTION'),
(N'PEMASAR', N'ROOT', NULL, N'Kantor Pemasar', N'OTHER'),
(N'HEAD_OFFICE', N'DIR_01', NULL, N'Direktur Utama', N'DIRECTORATE'),
(N'HEAD_OFFICE', N'DIR_02', NULL, N'Direktur Operasional', N'DIRECTORATE'),
(N'HEAD_OFFICE', N'DIR_03', NULL, N'Direktur Pemasaran', N'DIRECTORATE'),
(N'HEAD_OFFICE', N'DIV_01', N'DIR_02', N'Divisi Keuangan & Akuntansi', N'DIVISION'),
(N'HEAD_OFFICE', N'DIV_02', N'DIR_02', N'Aktuaris Perusahaan', N'DIVISION'),
(N'HEAD_OFFICE', N'DIV_03', N'DIR_02', N'Divisi Akseptasi & Reasuransi', N'DIVISION'),
(N'HEAD_OFFICE', N'DIV_04', N'DIR_02', N'Divisi Klaim & Subrogasi', N'DIVISION'),
(N'HEAD_OFFICE', N'DIV_05', N'DIR_02', N'Divisi SDM & Umum', N'DIVISION'),
(N'HEAD_OFFICE', N'DIV_06', N'DIR_01', N'Sekretaris Perusahaan', N'DIVISION'),
(N'HEAD_OFFICE', N'DIV_07', N'DIR_01', N'Divisi Perencanaan & Strategi Korporasi', N'DIVISION'),
(N'HEAD_OFFICE', N'DIV_08', N'DIR_01', N'Divisi Satuan Pengawasan Internal', N'DIVISION'),
(N'HEAD_OFFICE', N'DIV_09', N'DIR_01', N'Divisi Manajemen Risiko, Kepatuhan & Hukum', N'DIVISION'),
(N'HEAD_OFFICE', N'DIV_10', N'DIR_03', N'Divisi Pemasaran', N'DIVISION'),
(N'HEAD_OFFICE', N'DIV_11', N'DIR_03', N'Divisi Teknologi Informasi', N'DIVISION'),
(N'HEAD_OFFICE', N'BAG_01', N'DIV_01', N'Bagian Keuangan & Investasi', N'DEPARTMENT'),
(N'HEAD_OFFICE', N'BAG_02', N'DIV_01', N'Bagian Akuntansi & Pajak', N'DEPARTMENT'),
(N'HEAD_OFFICE', N'BAG_03', N'DIV_02', N'Bagian Aktuaria & Statistik', N'DEPARTMENT'),
(N'HEAD_OFFICE', N'BAG_04', N'DIV_03', N'Bagian Akseptasi Asuransi Pembiayaan', N'DEPARTMENT'),
(N'HEAD_OFFICE', N'BAG_05', N'DIV_03', N'Bagian Akseptasi Asuransi Non Pembiayaan & Reasuransi', N'DEPARTMENT'),
(N'HEAD_OFFICE', N'BAG_06', N'DIV_04', N'Bagian Klaim Asuransi Pembiayaan', N'DEPARTMENT'),
(N'HEAD_OFFICE', N'BAG_07', N'DIV_04', N'Bagian Klaim Asuransi Non Pembiayaan', N'DEPARTMENT'),
(N'HEAD_OFFICE', N'BAG_08', N'DIV_04', N'Bagian Subrogasi', N'DEPARTMENT'),
(N'HEAD_OFFICE', N'BAG_09', N'DIV_05', N'Bagian SDM', N'DEPARTMENT'),
(N'HEAD_OFFICE', N'BAG_10', N'DIV_05', N'Bagian Umum', N'DEPARTMENT'),
(N'HEAD_OFFICE', N'BAG_11', N'DIV_06', N'Bagian Sekretariat', N'DEPARTMENT'),
(N'HEAD_OFFICE', N'BAG_12', N'DIV_06', N'Bagian Humas & Protokol', N'DEPARTMENT'),
(N'HEAD_OFFICE', N'BAG_13', N'DIV_07', N'Bagian Perencanaan Korporasi', N'DEPARTMENT'),
(N'HEAD_OFFICE', N'BAG_14', N'DIV_07', N'Bagian Strategi Korporasi', N'DEPARTMENT'),
(N'HEAD_OFFICE', N'BAG_15', N'DIV_08', N'Bagian Satuan Pengawasan Internal', N'DEPARTMENT'),
(N'HEAD_OFFICE', N'BAG_16', N'DIV_09', N'Bagian Manajemen Risiko', N'DEPARTMENT'),
(N'HEAD_OFFICE', N'BAG_17', N'DIV_09', N'Bagian Kepatuhan & Hukum', N'DEPARTMENT'),
(N'HEAD_OFFICE', N'BAG_18', N'DIV_09', N'Bagian UKPN & UPK dan APU PPT', N'DEPARTMENT'),
(N'HEAD_OFFICE', N'BAG_19', N'DIV_10', N'Bagian Pemasaran 1', N'DEPARTMENT'),
(N'HEAD_OFFICE', N'BAG_20', N'DIV_10', N'Bagian Pemasaran 2', N'DEPARTMENT'),
(N'HEAD_OFFICE', N'BAG_21', N'DIV_10', N'Bagian Pemasaran 3', N'DEPARTMENT'),
(N'HEAD_OFFICE', N'BAG_22', N'DIV_10', N'Bagian Pemasaran 4', N'DEPARTMENT'),
(N'HEAD_OFFICE', N'BAG_23', N'DIV_11', N'Bagian Pengembangan Teknologi Informasi', N'DEPARTMENT'),
(N'HEAD_OFFICE', N'BAG_24', N'DIV_11', N'Bagian Operasional & Perencanaan Teknologi Informasi', N'DEPARTMENT'),
(N'HEAD_OFFICE', N'SEK_01', N'BAG_01', N'Seksi Keuangan', N'SECTION'),
(N'HEAD_OFFICE', N'SEK_02', N'BAG_01', N'Seksi Investasi', N'SECTION'),
(N'HEAD_OFFICE', N'SEK_03', N'BAG_01', N'Seksi Inkaso', N'SECTION'),
(N'HEAD_OFFICE', N'SEK_04', N'BAG_02', N'Seksi Akuntansi', N'SECTION'),
(N'HEAD_OFFICE', N'SEK_05', N'BAG_02', N'Seksi Pajak', N'SECTION'),
(N'HEAD_OFFICE', N'SEK_06', N'BAG_03', N'Seksi Aktuaria', N'SECTION'),
(N'HEAD_OFFICE', N'SEK_07', N'BAG_03', N'Seksi Statistik', N'SECTION'),
(N'HEAD_OFFICE', N'SEK_08', N'BAG_04', N'Seksi Akseptasi Asuransi Pembiayaan Konsumtif CBC', N'SECTION'),
(N'HEAD_OFFICE', N'SEK_09', N'BAG_04', N'Seksi Akseptasi Asuransi Pembiayaan Konsumtif Non CBC', N'SECTION'),
(N'HEAD_OFFICE', N'SEK_10', N'BAG_04', N'Seksi Akseptasi Asuransi Pembiayaan Produktif & Suretyship', N'SECTION'),
(N'HEAD_OFFICE', N'SEK_11', N'BAG_04', N'Seksi Adm. Teknik', N'SECTION'),
(N'HEAD_OFFICE', N'SEK_12', N'BAG_05', N'Seksi Akseptasi Asuransi Marine', N'SECTION'),
(N'HEAD_OFFICE', N'SEK_13', N'BAG_05', N'Seksi Akseptasi Asuransi Non Marine', N'SECTION'),
(N'HEAD_OFFICE', N'SEK_14', N'BAG_05', N'Seksi Reasuransi', N'SECTION'),
(N'HEAD_OFFICE', N'SEK_15', N'BAG_06', N'Seksi Klaim Wilayah Jawa', N'SECTION'),
(N'HEAD_OFFICE', N'SEK_16', N'BAG_06', N'Seksi Klaim Wilayah Sumatera', N'SECTION'),
(N'HEAD_OFFICE', N'SEK_17', N'BAG_06', N'Seksi Klaim Wilayah Non Jawa & Sumatera', N'SECTION'),
(N'HEAD_OFFICE', N'SEK_18', N'BAG_07', N'Seksi Klaim Asuransi GA & Health', N'SECTION'),
(N'HEAD_OFFICE', N'SEK_19', N'BAG_07', N'Seksi Klaim Asuransi Non GA & Health', N'SECTION'),
(N'HEAD_OFFICE', N'SEK_20', N'BAG_08', N'Seksi Subrogasi', N'SECTION'),
(N'HEAD_OFFICE', N'SEK_21', N'BAG_09', N'Seksi Adm. Kepegawaian', N'SECTION'),
(N'HEAD_OFFICE', N'SEK_22', N'BAG_09', N'Seksi Pelatihan & Pengembangan', N'SECTION'),
(N'HEAD_OFFICE', N'SEK_23', N'BAG_10', N'Seksi Logistik', N'SECTION'),
(N'HEAD_OFFICE', N'SEK_24', N'BAG_10', N'Seksi Arsip & Pengelolaan Aset', N'SECTION'),
(N'HEAD_OFFICE', N'SEK_25', N'BAG_11', N'Seksi Sekretariat', N'SECTION'),
(N'HEAD_OFFICE', N'SEK_26', N'BAG_12', N'Seksi Humas & Protokol', N'SECTION'),
(N'HEAD_OFFICE', N'SEK_27', N'BAG_13', N'Seksi Perencanaan Korporasi', N'SECTION'),
(N'HEAD_OFFICE', N'SEK_28', N'BAG_14', N'Seksi Strategi Korporasi', N'SECTION'),
(N'HEAD_OFFICE', N'SEK_29', N'BAG_15', N'Seksi Satuan Pengawasan Internal 1', N'SECTION'),
(N'HEAD_OFFICE', N'SEK_30', N'BAG_15', N'Seksi Satuan Pengawasan Internal 2', N'SECTION'),
(N'HEAD_OFFICE', N'SEK_31', N'BAG_15', N'Seksi Satuan Pengawasan Internal 3', N'SECTION'),
(N'HEAD_OFFICE', N'SEK_32', N'BAG_16', N'Seksi Manajemen Risiko Transaksional', N'SECTION'),
(N'HEAD_OFFICE', N'SEK_33', N'BAG_16', N'Seksi Manajemen Risiko Non Transaksional', N'SECTION'),
(N'HEAD_OFFICE', N'SEK_34', N'BAG_17', N'Seksi Kepatuhan', N'SECTION'),
(N'HEAD_OFFICE', N'SEK_35', N'BAG_17', N'Seksi Hukum', N'SECTION'),
(N'HEAD_OFFICE', N'SEK_36', N'BAG_18', N'Seksi UKPN, UPK & APU PPT', N'SECTION'),
(N'HEAD_OFFICE', N'SEK_37', N'BAG_18', N'Unit Kerja Data Nasabah', N'SECTION'),
(N'HEAD_OFFICE', N'SEK_38', N'BAG_19', N'Seksi Pemasaran Wilayah Sumatera & Sulawesi', N'SECTION'),
(N'HEAD_OFFICE', N'SEK_39', N'BAG_19', N'Seksi Pemasaran Wilayah Non Sumatera & Sulawesi', N'SECTION'),
(N'HEAD_OFFICE', N'SEK_40', N'BAG_20', N'Seksi Pemasaran Non Broker BUS', N'SECTION'),
(N'HEAD_OFFICE', N'SEK_41', N'BAG_20', N'Seksi Pemasaran Broker BUS', N'SECTION'),
(N'HEAD_OFFICE', N'SEK_42', N'BAG_20', N'Seksi Pemasaran Travel', N'SECTION'),
(N'HEAD_OFFICE', N'SEK_43', N'BAG_21', N'Seksi Pemasaran Broker', N'SECTION'),
(N'HEAD_OFFICE', N'SEK_44', N'BAG_21', N'Seksi Pemasaran Agent & Direct', N'SECTION'),
(N'HEAD_OFFICE', N'SEK_45', N'BAG_22', N'Seksi Pemasaran Administrasi', N'SECTION'),
(N'HEAD_OFFICE', N'SEK_46', N'BAG_22', N'Seksi Pemasaran Pengembangan & Monitoring Produk', N'SECTION'),
(N'HEAD_OFFICE', N'SEK_47', N'BAG_23', N'Seksi Pengembangan Teknologi Informasi Pendukung Bisnis', N'SECTION'),
(N'HEAD_OFFICE', N'SEK_48', N'BAG_23', N'Seksi Pengembangan Teknologi Informasi Pendukung Operasional', N'SECTION'),
(N'HEAD_OFFICE', N'SEK_49', N'BAG_24', N'Seksi Perencanaan & Tata Kelola Teknologi Informasi', N'SECTION'),
(N'HEAD_OFFICE', N'SEK_50', N'BAG_24', N'Seksi Operasional Teknologi Informasi', N'SECTION'),
(N'HEAD_OFFICE', N'SEK_51', N'BAG_24', N'Seksi Database & Administrasi Teknologi Informasi', N'SECTION');
INSERT INTO @Positions (ClassificationCode, PositionCode, UnitCode, SupervisorCode, Name, Track, IsUnitHead, MaxIncumbents, LevelCode) VALUES
(N'CAB-I', N'HEAD', N'ROOT', NULL, N'Kepala Cabang', N'LINE', 1, 1, NULL),
(N'CAB-I', N'HEAD_MARKETING', N'MARKETING', N'HEAD', N'Kepala Seksi Pemasaran', N'LINE', 1, 1, NULL),
(N'CAB-I', N'HEAD_TECHNICAL', N'TECHNICAL', N'HEAD', N'Kepala Seksi Teknik', N'LINE', 1, 1, NULL),
(N'CAB-I', N'HEAD_FINANCE', N'FINANCE', N'HEAD', N'Kepala Seksi Keuangan, SDM, dan Umum', N'LINE', 1, 1, NULL),
(N'CAB-I', N'STAFF_MARKETING', N'MARKETING', N'HEAD_MARKETING', N'Pelaksana Pemasaran', N'LINE', 0, NULL, NULL),
(N'CAB-I', N'STAFF_UNDERWRITING', N'TECHNICAL', N'HEAD_TECHNICAL', N'Pelaksana Akseptasi', N'LINE', 0, NULL, NULL),
(N'CAB-I', N'STAFF_CLAIMS_SUBROGATION', N'TECHNICAL', N'HEAD_TECHNICAL', N'Pelaksana Klaim & Subrogasi', N'LINE', 0, NULL, NULL),
(N'CAB-I', N'STAFF_FINANCE', N'FINANCE', N'HEAD_FINANCE', N'Pelaksana Keuangan, SDM, dan Umum', N'LINE', 0, NULL, NULL),
(N'CAB-II', N'HEAD', N'ROOT', NULL, N'Kepala Cabang', N'LINE', 1, 1, NULL),
(N'CAB-II', N'HEAD_MARKETING', N'MARKETING', N'HEAD', N'Kepala Seksi Pemasaran', N'LINE', 1, 1, NULL),
(N'CAB-II', N'HEAD_OPERATIONS', N'OPERATIONS', N'HEAD', N'Kepala Seksi Operasional', N'LINE', 1, 1, NULL),
(N'CAB-II', N'STAFF_MARKETING', N'MARKETING', N'HEAD_MARKETING', N'Pelaksana Pemasaran', N'LINE', 0, NULL, NULL),
(N'CAB-II', N'STAFF_UNDERWRITING', N'OPERATIONS', N'HEAD_OPERATIONS', N'Pelaksana Akseptasi', N'LINE', 0, NULL, NULL),
(N'CAB-II', N'STAFF_CLAIMS_SUBROGATION', N'OPERATIONS', N'HEAD_OPERATIONS', N'Pelaksana Klaim & Subrogasi', N'LINE', 0, NULL, NULL),
(N'CAB-II', N'STAFF_FINANCE', N'OPERATIONS', N'HEAD_OPERATIONS', N'Pelaksana Keuangan, SDM, dan Umum', N'LINE', 0, NULL, NULL),
(N'CAB-III', N'HEAD', N'ROOT', NULL, N'Kepala Cabang', N'LINE', 1, 1, NULL),
(N'CAB-III', N'HEAD_OPERATIONS', N'OPERATIONS', N'HEAD', N'Kepala Seksi Operasional', N'LINE', 1, 1, NULL),
(N'CAB-III', N'STAFF_MARKETING', N'ROOT', N'HEAD', N'Pelaksana Pemasaran', N'LINE', 0, NULL, NULL),
(N'CAB-III', N'STAFF_UNDERWRITING', N'OPERATIONS', N'HEAD_OPERATIONS', N'Pelaksana Akseptasi', N'LINE', 0, NULL, NULL),
(N'CAB-III', N'STAFF_CLAIMS_SUBROGATION', N'OPERATIONS', N'HEAD_OPERATIONS', N'Pelaksana Klaim & Subrogasi', N'LINE', 0, NULL, NULL),
(N'CAB-III', N'STAFF_FINANCE', N'OPERATIONS', N'HEAD_OPERATIONS', N'Pelaksana Keuangan, SDM, dan Umum', N'LINE', 0, NULL, NULL),
(N'PEMASAR', N'HEAD', N'ROOT', NULL, N'Kepala Pemasar', N'LINE', 1, 1, NULL),
(N'PEMASAR', N'STAFF_ADMIN', N'ROOT', N'HEAD', N'Pelaksana Administrasi Pemasar', N'LINE', 0, NULL, NULL),
(N'PEMASAR', N'STAFF_FINANCE', N'ROOT', N'HEAD', N'Pelaksana Keuangan Pemasar', N'LINE', 0, NULL, NULL),
(N'HEAD_OFFICE', N'HEAD_DIR_01', N'DIR_01', NULL, N'Direktur Utama', N'LINE', 1, 1, N'DIREKTUR'),
(N'HEAD_OFFICE', N'HEAD_DIR_02', N'DIR_02', NULL, N'Direktur Operasional', N'LINE', 1, 1, N'DIREKTUR'),
(N'HEAD_OFFICE', N'HEAD_DIR_03', N'DIR_03', NULL, N'Direktur Pemasaran', N'LINE', 1, 1, N'DIREKTUR'),
(N'HEAD_OFFICE', N'HEAD_DIV_01', N'DIV_01', N'HEAD_DIR_02', N'Kepala Divisi Keuangan & Akuntansi', N'LINE', 1, 1, N'KADIV'),
(N'HEAD_OFFICE', N'HEAD_DIV_02', N'DIV_02', N'HEAD_DIR_02', N'Aktuaris Perusahaan', N'LINE', 1, 1, N'KADIV'),
(N'HEAD_OFFICE', N'HEAD_DIV_03', N'DIV_03', N'HEAD_DIR_02', N'Kepala Divisi Akseptasi & Reasuransi', N'LINE', 1, 1, N'KADIV'),
(N'HEAD_OFFICE', N'HEAD_DIV_04', N'DIV_04', N'HEAD_DIR_02', N'Kepala Divisi Klaim & Subrogasi', N'LINE', 1, 1, N'KADIV'),
(N'HEAD_OFFICE', N'HEAD_DIV_05', N'DIV_05', N'HEAD_DIR_02', N'Kepala Divisi SDM & Umum', N'LINE', 1, 1, N'KADIV'),
(N'HEAD_OFFICE', N'HEAD_DIV_06', N'DIV_06', N'HEAD_DIR_01', N'Sekretaris Perusahaan', N'LINE', 1, 1, N'KADIV'),
(N'HEAD_OFFICE', N'HEAD_DIV_07', N'DIV_07', N'HEAD_DIR_01', N'Kepala Divisi Perencanaan & Strategi Korporasi', N'LINE', 1, 1, N'KADIV'),
(N'HEAD_OFFICE', N'HEAD_DIV_08', N'DIV_08', N'HEAD_DIR_01', N'Kepala Divisi Satuan Pengawasan Internal', N'LINE', 1, 1, N'KADIV'),
(N'HEAD_OFFICE', N'HEAD_DIV_09', N'DIV_09', N'HEAD_DIR_01', N'Kepala Divisi Manajemen Risiko, Kepatuhan & Hukum', N'LINE', 1, 1, N'KADIV'),
(N'HEAD_OFFICE', N'HEAD_DIV_10', N'DIV_10', N'HEAD_DIR_03', N'Kepala Divisi Pemasaran', N'LINE', 1, 1, N'KADIV'),
(N'HEAD_OFFICE', N'HEAD_DIV_11', N'DIV_11', N'HEAD_DIR_03', N'Kepala Divisi Teknologi Informasi', N'LINE', 1, 1, N'KADIV'),
(N'HEAD_OFFICE', N'HEAD_BAG_01', N'BAG_01', N'HEAD_DIV_01', N'Kepala Bagian Keuangan & Investasi', N'LINE', 1, 1, N'KABAG'),
(N'HEAD_OFFICE', N'HEAD_BAG_02', N'BAG_02', N'HEAD_DIV_01', N'Kepala Bagian Akuntansi & Pajak', N'LINE', 1, 1, N'KABAG'),
(N'HEAD_OFFICE', N'HEAD_BAG_03', N'BAG_03', N'HEAD_DIV_02', N'Kepala Bagian Aktuaria & Statistik', N'LINE', 1, 1, N'KABAG'),
(N'HEAD_OFFICE', N'HEAD_BAG_04', N'BAG_04', N'HEAD_DIV_03', N'Kepala Bagian Akseptasi Asuransi Pembiayaan', N'LINE', 1, 1, N'KABAG'),
(N'HEAD_OFFICE', N'HEAD_BAG_05', N'BAG_05', N'HEAD_DIV_03', N'Kepala Bagian Akseptasi Asuransi Non Pembiayaan & Reasuransi', N'LINE', 1, 1, N'KABAG'),
(N'HEAD_OFFICE', N'HEAD_BAG_06', N'BAG_06', N'HEAD_DIV_04', N'Kepala Bagian Klaim Asuransi Pembiayaan', N'LINE', 1, 1, N'KABAG'),
(N'HEAD_OFFICE', N'HEAD_BAG_07', N'BAG_07', N'HEAD_DIV_04', N'Kepala Bagian Klaim Asuransi Non Pembiayaan', N'LINE', 1, 1, N'KABAG'),
(N'HEAD_OFFICE', N'HEAD_BAG_08', N'BAG_08', N'HEAD_DIV_04', N'Kepala Bagian Subrogasi', N'LINE', 1, 1, N'KABAG'),
(N'HEAD_OFFICE', N'HEAD_BAG_09', N'BAG_09', N'HEAD_DIV_05', N'Kepala Bagian SDM', N'LINE', 1, 1, N'KABAG'),
(N'HEAD_OFFICE', N'HEAD_BAG_10', N'BAG_10', N'HEAD_DIV_05', N'Kepala Bagian Umum', N'LINE', 1, 1, N'KABAG'),
(N'HEAD_OFFICE', N'HEAD_BAG_11', N'BAG_11', N'HEAD_DIV_06', N'Kepala Bagian Sekretariat', N'LINE', 1, 1, N'KABAG'),
(N'HEAD_OFFICE', N'HEAD_BAG_12', N'BAG_12', N'HEAD_DIV_06', N'Kepala Bagian Humas & Protokol', N'LINE', 1, 1, N'KABAG'),
(N'HEAD_OFFICE', N'HEAD_BAG_13', N'BAG_13', N'HEAD_DIV_07', N'Kepala Bagian Perencanaan Korporasi', N'LINE', 1, 1, N'KABAG'),
(N'HEAD_OFFICE', N'HEAD_BAG_14', N'BAG_14', N'HEAD_DIV_07', N'Kepala Bagian Strategi Korporasi', N'LINE', 1, 1, N'KABAG'),
(N'HEAD_OFFICE', N'HEAD_BAG_15', N'BAG_15', N'HEAD_DIV_08', N'Kepala Bagian Satuan Pengawasan Internal', N'LINE', 1, 1, N'KABAG'),
(N'HEAD_OFFICE', N'HEAD_BAG_16', N'BAG_16', N'HEAD_DIV_09', N'Kepala Bagian Manajemen Risiko', N'LINE', 1, 1, N'KABAG'),
(N'HEAD_OFFICE', N'HEAD_BAG_17', N'BAG_17', N'HEAD_DIV_09', N'Kepala Bagian Kepatuhan & Hukum', N'LINE', 1, 1, N'KABAG'),
(N'HEAD_OFFICE', N'HEAD_BAG_18', N'BAG_18', N'HEAD_DIV_09', N'Kepala Bagian UKPN & UPK dan APU PPT', N'LINE', 1, 1, N'KABAG'),
(N'HEAD_OFFICE', N'HEAD_BAG_19', N'BAG_19', N'HEAD_DIV_10', N'Kepala Bagian Pemasaran 1', N'LINE', 1, 1, N'KABAG'),
(N'HEAD_OFFICE', N'HEAD_BAG_20', N'BAG_20', N'HEAD_DIV_10', N'Kepala Bagian Pemasaran 2', N'LINE', 1, 1, N'KABAG'),
(N'HEAD_OFFICE', N'HEAD_BAG_21', N'BAG_21', N'HEAD_DIV_10', N'Kepala Bagian Pemasaran 3', N'LINE', 1, 1, N'KABAG'),
(N'HEAD_OFFICE', N'HEAD_BAG_22', N'BAG_22', N'HEAD_DIV_10', N'Kepala Bagian Pemasaran 4', N'LINE', 1, 1, N'KABAG'),
(N'HEAD_OFFICE', N'HEAD_BAG_23', N'BAG_23', N'HEAD_DIV_11', N'Kepala Bagian Pengembangan Teknologi Informasi', N'LINE', 1, 1, N'KABAG'),
(N'HEAD_OFFICE', N'HEAD_BAG_24', N'BAG_24', N'HEAD_DIV_11', N'Kepala Bagian Operasional & Perencanaan Teknologi Informasi', N'LINE', 1, 1, N'KABAG'),
(N'HEAD_OFFICE', N'HEAD_SEK_01', N'SEK_01', N'HEAD_BAG_01', N'Kepala Seksi Keuangan', N'LINE', 1, 1, N'KASI'),
(N'HEAD_OFFICE', N'HEAD_SEK_02', N'SEK_02', N'HEAD_BAG_01', N'Kepala Seksi Investasi', N'LINE', 1, 1, N'KASI'),
(N'HEAD_OFFICE', N'HEAD_SEK_03', N'SEK_03', N'HEAD_BAG_01', N'Kepala Seksi Inkaso', N'LINE', 1, 1, N'KASI'),
(N'HEAD_OFFICE', N'HEAD_SEK_04', N'SEK_04', N'HEAD_BAG_02', N'Kepala Seksi Akuntansi', N'LINE', 1, 1, N'KASI'),
(N'HEAD_OFFICE', N'HEAD_SEK_05', N'SEK_05', N'HEAD_BAG_02', N'Kepala Seksi Pajak', N'LINE', 1, 1, N'KASI'),
(N'HEAD_OFFICE', N'HEAD_SEK_06', N'SEK_06', N'HEAD_BAG_03', N'Kepala Seksi Aktuaria', N'LINE', 1, 1, N'KASI'),
(N'HEAD_OFFICE', N'HEAD_SEK_07', N'SEK_07', N'HEAD_BAG_03', N'Kepala Seksi Statistik', N'LINE', 1, 1, N'KASI'),
(N'HEAD_OFFICE', N'HEAD_SEK_08', N'SEK_08', N'HEAD_BAG_04', N'Kepala Seksi Akseptasi Asuransi Pembiayaan Konsumtif CBC', N'LINE', 1, 1, N'KASI'),
(N'HEAD_OFFICE', N'HEAD_SEK_09', N'SEK_09', N'HEAD_BAG_04', N'Kepala Seksi Akseptasi Asuransi Pembiayaan Konsumtif Non CBC', N'LINE', 1, 1, N'KASI'),
(N'HEAD_OFFICE', N'HEAD_SEK_10', N'SEK_10', N'HEAD_BAG_04', N'Kepala Seksi Akseptasi Asuransi Pembiayaan Produktif & Suretyship', N'LINE', 1, 1, N'KASI'),
(N'HEAD_OFFICE', N'HEAD_SEK_11', N'SEK_11', N'HEAD_BAG_04', N'Kepala Seksi Adm. Teknik', N'LINE', 1, 1, N'KASI'),
(N'HEAD_OFFICE', N'HEAD_SEK_12', N'SEK_12', N'HEAD_BAG_05', N'Kepala Seksi Akseptasi Asuransi Marine', N'LINE', 1, 1, N'KASI'),
(N'HEAD_OFFICE', N'HEAD_SEK_13', N'SEK_13', N'HEAD_BAG_05', N'Kepala Seksi Akseptasi Asuransi Non Marine', N'LINE', 1, 1, N'KASI'),
(N'HEAD_OFFICE', N'HEAD_SEK_14', N'SEK_14', N'HEAD_BAG_05', N'Kepala Seksi Reasuransi', N'LINE', 1, 1, N'KASI'),
(N'HEAD_OFFICE', N'HEAD_SEK_15', N'SEK_15', N'HEAD_BAG_06', N'Kepala Seksi Klaim Wilayah Jawa', N'LINE', 1, 1, N'KASI'),
(N'HEAD_OFFICE', N'HEAD_SEK_16', N'SEK_16', N'HEAD_BAG_06', N'Kepala Seksi Klaim Wilayah Sumatera', N'LINE', 1, 1, N'KASI'),
(N'HEAD_OFFICE', N'HEAD_SEK_17', N'SEK_17', N'HEAD_BAG_06', N'Kepala Seksi Klaim Wilayah Non Jawa & Sumatera', N'LINE', 1, 1, N'KASI'),
(N'HEAD_OFFICE', N'HEAD_SEK_18', N'SEK_18', N'HEAD_BAG_07', N'Kepala Seksi Klaim Asuransi GA & Health', N'LINE', 1, 1, N'KASI'),
(N'HEAD_OFFICE', N'HEAD_SEK_19', N'SEK_19', N'HEAD_BAG_07', N'Kepala Seksi Klaim Asuransi Non GA & Health', N'LINE', 1, 1, N'KASI'),
(N'HEAD_OFFICE', N'HEAD_SEK_20', N'SEK_20', N'HEAD_BAG_08', N'Kepala Seksi Subrogasi', N'LINE', 1, 1, N'KASI'),
(N'HEAD_OFFICE', N'HEAD_SEK_21', N'SEK_21', N'HEAD_BAG_09', N'Kepala Seksi Adm. Kepegawaian', N'LINE', 1, 1, N'KASI'),
(N'HEAD_OFFICE', N'HEAD_SEK_22', N'SEK_22', N'HEAD_BAG_09', N'Kepala Seksi Pelatihan & Pengembangan', N'LINE', 1, 1, N'KASI'),
(N'HEAD_OFFICE', N'HEAD_SEK_23', N'SEK_23', N'HEAD_BAG_10', N'Kepala Seksi Logistik', N'LINE', 1, 1, N'KASI'),
(N'HEAD_OFFICE', N'HEAD_SEK_24', N'SEK_24', N'HEAD_BAG_10', N'Kepala Seksi Arsip & Pengelolaan Aset', N'LINE', 1, 1, N'KASI'),
(N'HEAD_OFFICE', N'HEAD_SEK_25', N'SEK_25', N'HEAD_BAG_11', N'Kepala Seksi Sekretariat', N'LINE', 1, 1, N'KASI'),
(N'HEAD_OFFICE', N'HEAD_SEK_26', N'SEK_26', N'HEAD_BAG_12', N'Kepala Seksi Humas & Protokol', N'LINE', 1, 1, N'KASI'),
(N'HEAD_OFFICE', N'HEAD_SEK_27', N'SEK_27', N'HEAD_BAG_13', N'Kepala Seksi Perencanaan Korporasi', N'LINE', 1, 1, N'KASI'),
(N'HEAD_OFFICE', N'HEAD_SEK_28', N'SEK_28', N'HEAD_BAG_14', N'Kepala Seksi Strategi Korporasi', N'LINE', 1, 1, N'KASI'),
(N'HEAD_OFFICE', N'HEAD_SEK_29', N'SEK_29', N'HEAD_BAG_15', N'Kepala Seksi Satuan Pengawasan Internal 1', N'LINE', 1, 1, N'KASI'),
(N'HEAD_OFFICE', N'HEAD_SEK_30', N'SEK_30', N'HEAD_BAG_15', N'Kepala Seksi Satuan Pengawasan Internal 2', N'LINE', 1, 1, N'KASI'),
(N'HEAD_OFFICE', N'HEAD_SEK_31', N'SEK_31', N'HEAD_BAG_15', N'Kepala Seksi Satuan Pengawasan Internal 3', N'LINE', 1, 1, N'KASI'),
(N'HEAD_OFFICE', N'HEAD_SEK_32', N'SEK_32', N'HEAD_BAG_16', N'Kepala Seksi Manajemen Risiko Transaksional', N'LINE', 1, 1, N'KASI'),
(N'HEAD_OFFICE', N'HEAD_SEK_33', N'SEK_33', N'HEAD_BAG_16', N'Kepala Seksi Manajemen Risiko Non Transaksional', N'LINE', 1, 1, N'KASI'),
(N'HEAD_OFFICE', N'HEAD_SEK_34', N'SEK_34', N'HEAD_BAG_17', N'Kepala Seksi Kepatuhan', N'LINE', 1, 1, N'KASI'),
(N'HEAD_OFFICE', N'HEAD_SEK_35', N'SEK_35', N'HEAD_BAG_17', N'Kepala Seksi Hukum', N'LINE', 1, 1, N'KASI'),
(N'HEAD_OFFICE', N'HEAD_SEK_36', N'SEK_36', N'HEAD_BAG_18', N'Kepala Seksi UKPN, UPK & APU PPT', N'LINE', 1, 1, N'KASI'),
(N'HEAD_OFFICE', N'HEAD_SEK_37', N'SEK_37', N'HEAD_BAG_18', N'Kepala Unit Kerja Data Nasabah', N'LINE', 1, 1, N'KASI'),
(N'HEAD_OFFICE', N'HEAD_SEK_38', N'SEK_38', N'HEAD_BAG_19', N'Kepala Seksi Pemasaran Wilayah Sumatera & Sulawesi', N'LINE', 1, 1, N'KASI'),
(N'HEAD_OFFICE', N'HEAD_SEK_39', N'SEK_39', N'HEAD_BAG_19', N'Kepala Seksi Pemasaran Wilayah Non Sumatera & Sulawesi', N'LINE', 1, 1, N'KASI'),
(N'HEAD_OFFICE', N'HEAD_SEK_40', N'SEK_40', N'HEAD_BAG_20', N'Kepala Seksi Pemasaran Non Broker BUS', N'LINE', 1, 1, N'KASI'),
(N'HEAD_OFFICE', N'HEAD_SEK_41', N'SEK_41', N'HEAD_BAG_20', N'Kepala Seksi Pemasaran Broker BUS', N'LINE', 1, 1, N'KASI'),
(N'HEAD_OFFICE', N'HEAD_SEK_42', N'SEK_42', N'HEAD_BAG_20', N'Kepala Seksi Pemasaran Travel', N'LINE', 1, 1, N'KASI'),
(N'HEAD_OFFICE', N'HEAD_SEK_43', N'SEK_43', N'HEAD_BAG_21', N'Kepala Seksi Pemasaran Broker', N'LINE', 1, 1, N'KASI'),
(N'HEAD_OFFICE', N'HEAD_SEK_44', N'SEK_44', N'HEAD_BAG_21', N'Kepala Seksi Pemasaran Agent & Direct', N'LINE', 1, 1, N'KASI'),
(N'HEAD_OFFICE', N'HEAD_SEK_45', N'SEK_45', N'HEAD_BAG_22', N'Kepala Seksi Pemasaran Administrasi', N'LINE', 1, 1, N'KASI'),
(N'HEAD_OFFICE', N'HEAD_SEK_46', N'SEK_46', N'HEAD_BAG_22', N'Kepala Seksi Pemasaran Pengembangan & Monitoring Produk', N'LINE', 1, 1, N'KASI'),
(N'HEAD_OFFICE', N'HEAD_SEK_47', N'SEK_47', N'HEAD_BAG_23', N'Kepala Seksi Pengembangan Teknologi Informasi Pendukung Bisnis', N'LINE', 1, 1, N'KASI'),
(N'HEAD_OFFICE', N'HEAD_SEK_48', N'SEK_48', N'HEAD_BAG_23', N'Kepala Seksi Pengembangan Teknologi Informasi Pendukung Operasional', N'LINE', 1, 1, N'KASI'),
(N'HEAD_OFFICE', N'HEAD_SEK_49', N'SEK_49', N'HEAD_BAG_24', N'Kepala Seksi Perencanaan & Tata Kelola Teknologi Informasi', N'LINE', 1, 1, N'KASI'),
(N'HEAD_OFFICE', N'HEAD_SEK_50', N'SEK_50', N'HEAD_BAG_24', N'Kepala Seksi Operasional Teknologi Informasi', N'LINE', 1, 1, N'KASI'),
(N'HEAD_OFFICE', N'HEAD_SEK_51', N'SEK_51', N'HEAD_BAG_24', N'Kepala Seksi Database & Administrasi Teknologi Informasi', N'LINE', 1, 1, N'KASI');

BEGIN TRY
    BEGIN TRANSACTION;
    DECLARE @AdminAppId uniqueidentifier;
    SELECT @AdminAppId = Id FROM dbo.ClientApps WITH (UPDLOCK, HOLDLOCK)
    WHERE ClientId = N'auth-admin' AND IsActive = 1;
    IF @AdminAppId IS NULL OR NOT EXISTS (
        SELECT 1 FROM dbo.Users u WITH (UPDLOCK, HOLDLOCK)
        JOIN dbo.UserAppRoles ur WITH (UPDLOCK, HOLDLOCK) ON ur.UserId = u.Id
        JOIN dbo.Roles r WITH (UPDLOCK, HOLDLOCK) ON r.Id = ur.RoleId AND r.ClientAppId = ur.ClientAppId
        WHERE u.Id = @ActorUserId AND u.IsActive = 1 AND u.IsAccountVerified = 1
          AND (u.LockoutEndUtc IS NULL OR u.LockoutEndUtc <= SYSUTCDATETIME())
          AND ur.ClientAppId = @AdminAppId AND r.Name = N'SuperAdmin' AND r.IsSystemRole = 1
    ) THROW 51101, 'Actor must be an active verified auth-admin system SuperAdmin.', 1;

    IF EXISTS (
        SELECT 1 FROM @Units u WHERE NOT EXISTS (
            SELECT 1 FROM dbo.OfficeClassifications c WITH (UPDLOCK, HOLDLOCK) WHERE c.Code = u.ClassificationCode
        )
    ) THROW 51102, 'Provision office classifications first.', 1;

    INSERT INTO @Templates
    SELECT c.ClassificationCode, COALESCE(t.Id, NEWID()), CASE WHEN t.Id IS NULL THEN 1 ELSE 0 END
    FROM (SELECT DISTINCT ClassificationCode FROM @Units) c
    LEFT JOIN dbo.StructureTemplates t WITH (UPDLOCK, HOLDLOCK)
        ON t.ClassificationCode = c.ClassificationCode AND t.Version = 1;

    IF EXISTS (
        SELECT 1 FROM @Templates e JOIN dbo.StructureTemplates t WITH (UPDLOCK, HOLDLOCK)
            ON t.ClassificationCode = e.ClassificationCode
        WHERE e.IsNew = 1 AND t.Version <> 1
    ) THROW 51103, 'A conflicting template version exists; review manually.', 1;

    UPDATE e SET UnitId = COALESCE(u.Id, NEWID()) FROM @Units e
    JOIN @Templates t ON t.ClassificationCode = e.ClassificationCode
    LEFT JOIN dbo.TemplateOrgUnits u WITH (UPDLOCK, HOLDLOCK) ON u.TemplateId = t.TemplateId AND u.UnitCode = e.UnitCode;
    UPDATE e SET PositionId = COALESCE(p.Id, NEWID()) FROM @Positions e
    JOIN @Templates t ON t.ClassificationCode = e.ClassificationCode
    LEFT JOIN dbo.TemplatePositions p WITH (UPDLOCK, HOLDLOCK) ON p.TemplateId = t.TemplateId AND p.PositionCode = e.PositionCode;

    -- Two-way comparisons reject incomplete or altered existing templates instead
    -- of modifying drafts, published versions, or legitimate office structures.
    IF EXISTS (
        SELECT e.ClassificationCode, e.UnitCode, e.ParentCode, e.Name COLLATE Latin1_General_100_BIN2, e.UnitType
        FROM @Units e JOIN @Templates t ON t.ClassificationCode = e.ClassificationCode WHERE t.IsNew = 0
        EXCEPT
        SELECT t.ClassificationCode, u.UnitCode, parent.UnitCode, u.Name COLLATE Latin1_General_100_BIN2, u.UnitType
        FROM @Templates t JOIN dbo.TemplateOrgUnits u WITH (HOLDLOCK) ON u.TemplateId = t.TemplateId
        LEFT JOIN dbo.TemplateOrgUnits parent WITH (HOLDLOCK) ON parent.Id = u.ParentTemplateOrgUnitId WHERE t.IsNew = 0
    ) OR EXISTS (
        SELECT t.ClassificationCode, u.UnitCode, parent.UnitCode, u.Name COLLATE Latin1_General_100_BIN2, u.UnitType
        FROM @Templates t JOIN dbo.TemplateOrgUnits u WITH (HOLDLOCK) ON u.TemplateId = t.TemplateId
        LEFT JOIN dbo.TemplateOrgUnits parent WITH (HOLDLOCK) ON parent.Id = u.ParentTemplateOrgUnitId WHERE t.IsNew = 0
        EXCEPT
        SELECT e.ClassificationCode, e.UnitCode, e.ParentCode, e.Name COLLATE Latin1_General_100_BIN2, e.UnitType
        FROM @Units e JOIN @Templates t ON t.ClassificationCode = e.ClassificationCode WHERE t.IsNew = 0
    ) THROW 51104, 'Existing template units differ; no data was overwritten.', 1;

    IF EXISTS (
        SELECT e.ClassificationCode, e.PositionCode, e.UnitCode, e.SupervisorCode, e.Name COLLATE Latin1_General_100_BIN2,
               e.Track, e.IsUnitHead, e.MaxIncumbents, e.LevelCode
        FROM @Positions e JOIN @Templates t ON t.ClassificationCode = e.ClassificationCode WHERE t.IsNew = 0
        EXCEPT
        SELECT t.ClassificationCode, p.PositionCode, u.UnitCode, supervisor.PositionCode, p.Name COLLATE Latin1_General_100_BIN2,
               p.Track, p.IsUnitHead, p.MaxIncumbents, lv.Code
        FROM @Templates t JOIN dbo.TemplatePositions p WITH (HOLDLOCK) ON p.TemplateId = t.TemplateId
        JOIN dbo.TemplateOrgUnits u WITH (HOLDLOCK) ON u.Id = p.TemplateOrgUnitId
        LEFT JOIN dbo.TemplatePositions supervisor WITH (HOLDLOCK) ON supervisor.Id = p.ReportsToTemplatePositionId
        LEFT JOIN dbo.PositionLevels lv WITH (HOLDLOCK) ON lv.Id = p.LevelId WHERE t.IsNew = 0
    ) OR EXISTS (
        SELECT t.ClassificationCode, p.PositionCode, u.UnitCode, supervisor.PositionCode, p.Name COLLATE Latin1_General_100_BIN2,
               p.Track, p.IsUnitHead, p.MaxIncumbents, lv.Code
        FROM @Templates t JOIN dbo.TemplatePositions p WITH (HOLDLOCK) ON p.TemplateId = t.TemplateId
        JOIN dbo.TemplateOrgUnits u WITH (HOLDLOCK) ON u.Id = p.TemplateOrgUnitId
        LEFT JOIN dbo.TemplatePositions supervisor WITH (HOLDLOCK) ON supervisor.Id = p.ReportsToTemplatePositionId
        LEFT JOIN dbo.PositionLevels lv WITH (HOLDLOCK) ON lv.Id = p.LevelId WHERE t.IsNew = 0
        EXCEPT
        SELECT e.ClassificationCode, e.PositionCode, e.UnitCode, e.SupervisorCode, e.Name COLLATE Latin1_General_100_BIN2,
               e.Track, e.IsUnitHead, e.MaxIncumbents, e.LevelCode
        FROM @Positions e JOIN @Templates t ON t.ClassificationCode = e.ClassificationCode WHERE t.IsNew = 0
    ) THROW 51105, 'Existing template positions differ; no data was overwritten.', 1;

    IF EXISTS (
        SELECT 1 FROM @Levels l JOIN dbo.PositionLevels p WITH (UPDLOCK, HOLDLOCK) ON p.Code = l.LevelCode
        WHERE p.Name COLLATE Latin1_General_100_BIN2 <> l.Name COLLATE Latin1_General_100_BIN2 OR p.DisplayOrder <> l.DisplayOrder
    ) THROW 51106, 'Existing position levels differ; no data was overwritten.', 1;

    DECLARE @InsertedLevels int, @InsertedTemplates int, @InsertedUnits int, @InsertedPositions int;
    INSERT INTO dbo.PositionLevels (Id, Code, Name, DisplayOrder, CreatedAtUtc, CreatedByUserId)
    SELECT NEWID(), l.LevelCode, l.Name, l.DisplayOrder, SYSUTCDATETIME(), @ActorUserId
    FROM @Levels l
    WHERE NOT EXISTS (SELECT 1 FROM dbo.PositionLevels p WITH (UPDLOCK, HOLDLOCK) WHERE p.Code = l.LevelCode);
    SET @InsertedLevels = @@ROWCOUNT;

    IF EXISTS (
        SELECT 1 FROM @Positions e
        WHERE e.LevelCode IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.PositionLevels p WHERE p.Code = e.LevelCode)
    ) THROW 51107, 'Position level code not found; no data was written.', 1;
    INSERT INTO dbo.StructureTemplates
        (Id, ClassificationCode, Version, State, Notes, CreatedAtUtc, CreatedByUserId, PublishedAtUtc, PublishedByUserId)
    SELECT TemplateId, ClassificationCode, 1, 'DRAFT', N'Template operasional awal dan template Kantor Pusat (HEAD_OFFICE, hanya jabatan pimpinan unit sesuai data resmi pengguna) berstatus draft. Kode unit dan posisi adalah kode teknis usulan, bukan kode resmi perusahaan. ROOT hanya wadah struktur template dan bukan unit master perusahaan yang telah disahkan. Level posisi hanya dipetakan pada template HEAD_OFFICE; template operasional belum dipetakan karena strukturnya terpisah. Template ini tidak menetapkan role atau permission authorization.', SYSUTCDATETIME(), @ActorUserId, NULL, NULL
    FROM @Templates WHERE IsNew = 1;
    SET @InsertedTemplates = @@ROWCOUNT;

    INSERT INTO dbo.TemplateOrgUnits (Id, TemplateId, ParentTemplateOrgUnitId, UnitCode, UnitType, Name)
    SELECT e.UnitId, t.TemplateId, parent.UnitId, e.UnitCode, e.UnitType, e.Name
    FROM @Units e JOIN @Templates t ON t.ClassificationCode = e.ClassificationCode
    LEFT JOIN @Units parent ON parent.ClassificationCode = e.ClassificationCode AND parent.UnitCode = e.ParentCode
    WHERE t.IsNew = 1;
    SET @InsertedUnits = @@ROWCOUNT;

    INSERT INTO dbo.TemplatePositions
        (Id, TemplateId, TemplateOrgUnitId, ReportsToTemplatePositionId, LevelId, PositionCode, Name, Track, IsUnitHead, MaxIncumbents)
    SELECT e.PositionId, t.TemplateId, u.UnitId, supervisor.PositionId, lv.Id,
           e.PositionCode, e.Name, e.Track, e.IsUnitHead, e.MaxIncumbents
    FROM @Positions e JOIN @Templates t ON t.ClassificationCode = e.ClassificationCode
    JOIN @Units u ON u.ClassificationCode = e.ClassificationCode AND u.UnitCode = e.UnitCode
    LEFT JOIN @Positions supervisor ON supervisor.ClassificationCode = e.ClassificationCode AND supervisor.PositionCode = e.SupervisorCode
    LEFT JOIN dbo.PositionLevels lv ON lv.Code = e.LevelCode
    WHERE t.IsNew = 1;
    SET @InsertedPositions = @@ROWCOUNT;

    DECLARE @DetailJson nvarchar(4000);
    SET @DetailJson = N'{"actorUserId":"' + CONVERT(nvarchar(36), @ActorUserId)
        + N'","templateVersion":1,"insertedLevels":' + CONVERT(nvarchar(11), @InsertedLevels)
        + N',"insertedTemplates":' + CONVERT(nvarchar(11), @InsertedTemplates)
        + N',"insertedUnits":' + CONVERT(nvarchar(11), @InsertedUnits)
        + N',"insertedPositions":' + CONVERT(nvarchar(11), @InsertedPositions)
        + N',"published":false,"appliedToOffices":false,"rolesChanged":false}';
    INSERT INTO dbo.AuditLogs (Id, TimestampUtc, UserId, ClientAppId, EventType, IsSuccess, IpAddress, UserAgent, DetailJson)
    VALUES (NEWID(), SYSUTCDATETIME(), @ActorUserId, @AdminAppId, N'OrganizationTemplatesProvisioned',
            1, NULL, N'operator-sqlcmd-provisioning', @DetailJson);
    COMMIT TRANSACTION;
    SELECT @InsertedLevels AS InsertedLevels, @InsertedTemplates AS InsertedTemplates, @InsertedUnits AS InsertedUnits, @InsertedPositions AS InsertedPositions;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
