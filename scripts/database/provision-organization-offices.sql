-- Operator-run only. No application startup or migration invokes this script.
-- Requires SQLCMD variables EffectiveFrom (yyyy-MM-dd), ActorUserId (GUID).
-- Review the target server/database and the supplied business date before running.
-- The business date is deliberately NOT inferred from GETDATE or deployment time.
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @EffectiveFrom date = TRY_CONVERT(date, '$(EffectiveFrom)', 23);
DECLARE @ActorUserId uniqueidentifier = TRY_CONVERT(uniqueidentifier, '$(ActorUserId)');
IF @EffectiveFrom IS NULL OR @ActorUserId IS NULL
    THROW 51000, 'Provide valid EffectiveFrom and ActorUserId SQLCMD variables.', 1;

DECLARE @Classifications TABLE (Code varchar(20) PRIMARY KEY, DisplayName nvarchar(200));
INSERT INTO @Classifications VALUES
('HEAD_OFFICE', N'Kantor Pusat'),
('CAB-I', N'Kantor Cabang Kelas I'),
('CAB-II', N'Kantor Cabang Kelas II'),
('CAB-III', N'Kantor Cabang Kelas III'),
('PEMASAR', N'Kantor Pemasar');

DECLARE @Master TABLE (OfficeCode varchar(3) PRIMARY KEY, Name nvarchar(200), ClassificationCode varchar(20));
INSERT INTO @Master VALUES
('000', N'KANTOR PUSAT', 'HEAD_OFFICE'),
('110', N'CABANG SYARIAH ACEH', 'CAB-II'),
('111', N'CABANG SYARIAH MEDAN', 'CAB-II'),
('112', N'CABANG SYARIAH PADANG', 'CAB-III'),
('113', N'CABANG SYARIAH PEKANBARU', 'CAB-II'),
('114', N'PEMASAR SYARIAH JAMBI', 'PEMASAR'),
('115', N'PEMASAR SYARIAH PALEMBANG', 'PEMASAR'),
('116', N'PEMASAR SYARIAH BENGKULU', 'PEMASAR'),
('117', N'PEMASAR SYARIAH LAMPUNG', 'PEMASAR'),
('119', N'PEMASAR SYARIAH LHOKSEUMAWE', 'PEMASAR'),
('220', N'CABANG SYARIAH JAKARTA', 'CAB-I'),
('221', N'CABANG SYARIAH BANDUNG', 'CAB-II'),
('222', N'CABANG SYARIAH SEMARANG', 'CAB-III'),
('223', N'PEMASAR SYARIAH YOGYAKARTA', 'PEMASAR'),
('224', N'CABANG SYARIAH SURABAYA', 'CAB-II'),
('225', N'PEMASAR SERANG', 'PEMASAR'),
('226', N'PEMASAR SYARIAH CIREBON', 'PEMASAR'),
('227', N'PEMASAR SYARIAH TASIKMALAYA', 'PEMASAR'),
('228', N'PEMASAR SYARIAH KEDIRI', 'PEMASAR'),
('330', N'CABANG SYARIAH PONTIANAK', 'CAB-III'),
('331', N'PEMASAR SYARIAH PALANGKARAYA', 'PEMASAR'),
('332', N'PEMASAR SYARIAH BANJARMASIN', 'PEMASAR'),
('333', N'CABANG SYARIAH SAMARINDA', 'CAB-III'),
('440', N'PEMASAR SYARIAH MANADO', 'PEMASAR'),
('441', N'PEMASAR SYARIAH PALU', 'PEMASAR'),
('443', N'CABANG SYARIAH MAKASSAR', 'CAB-II'),
('550', N'PEMASAR SYARIAH BALI', 'PEMASAR'),
('551', N'CABANG SYARIAH MATARAM', 'CAB-II');

BEGIN TRY
    BEGIN TRANSACTION;

    -- Lock the actor and membership while authorizing this explicit bootstrap action.
    DECLARE @AdminAppId uniqueidentifier;
    SELECT @AdminAppId = Id FROM dbo.ClientApps WITH (UPDLOCK, HOLDLOCK)
    WHERE ClientId = N'auth-admin' AND IsActive = 1;

    IF @AdminAppId IS NULL OR NOT EXISTS (
        SELECT 1 FROM dbo.Users u WITH (UPDLOCK, HOLDLOCK)
        INNER JOIN dbo.UserAppRoles ur WITH (UPDLOCK, HOLDLOCK) ON ur.UserId = u.Id
        INNER JOIN dbo.Roles r WITH (UPDLOCK, HOLDLOCK) ON r.Id = ur.RoleId AND r.ClientAppId = ur.ClientAppId
        WHERE u.Id = @ActorUserId AND u.IsActive = 1 AND u.IsAccountVerified = 1
          AND (u.LockoutEndUtc IS NULL OR u.LockoutEndUtc <= SYSUTCDATETIME())
          AND ur.ClientAppId = @AdminAppId AND r.Name = N'SuperAdmin' AND r.IsSystemRole = 1
    )
        THROW 51001, 'Actor must be an active verified auth-admin system SuperAdmin.', 1;

    -- Never overwrite existing master definitions or effective-date history.
    IF EXISTS (
        SELECT 1 FROM dbo.OfficeClassifications c WITH (UPDLOCK, HOLDLOCK)
        INNER JOIN @Classifications m ON c.Code = m.Code
        WHERE c.DisplayName COLLATE Latin1_General_100_BIN2 <> m.DisplayName COLLATE Latin1_General_100_BIN2
    )
        THROW 51002, 'Classification differs from baseline; review manually before provisioning.', 1;

    DECLARE @InsertedClassifications int, @InsertedOffices int, @InsertedVersions int;
    INSERT INTO dbo.OfficeClassifications (Code, DisplayName)
    SELECT m.Code, m.DisplayName FROM @Classifications m
    WHERE NOT EXISTS (SELECT 1 FROM dbo.OfficeClassifications c WITH (UPDLOCK, HOLDLOCK) WHERE c.Code = m.Code);
    SET @InsertedClassifications = @@ROWCOUNT;

    INSERT INTO dbo.Offices (Id, OfficeCode, CreatedAtUtc, CreatedByUserId)
    SELECT NEWID(), m.OfficeCode, SYSUTCDATETIME(), @ActorUserId FROM @Master m
    WHERE NOT EXISTS (SELECT 1 FROM dbo.Offices o WITH (UPDLOCK, HOLDLOCK) WHERE o.OfficeCode = m.OfficeCode);
    SET @InsertedOffices = @@ROWCOUNT;

    -- A pre-existing office is only accepted if its exact initial version matches.
    -- Later legitimate versions are retained and never reset by a retry.
    IF EXISTS (
        SELECT 1 FROM @Master m
        INNER JOIN dbo.Offices o WITH (UPDLOCK, HOLDLOCK) ON o.OfficeCode = m.OfficeCode
        WHERE EXISTS (SELECT 1 FROM dbo.OfficeVersions v WITH (UPDLOCK, HOLDLOCK) WHERE v.OfficeId = o.Id)
          AND NOT EXISTS (
              SELECT 1 FROM dbo.OfficeVersions v WITH (UPDLOCK, HOLDLOCK)
              WHERE v.OfficeId = o.Id AND v.EffectiveFrom = @EffectiveFrom AND v.RevisionNumber = 1 AND v.IsActive = 1
                AND v.Name COLLATE Latin1_General_100_BIN2 = m.Name COLLATE Latin1_General_100_BIN2
                AND v.ClassificationCode = m.ClassificationCode
          )
    )
        THROW 51003, 'Existing office history differs from initial baseline; no data was overwritten.', 1;

    INSERT INTO dbo.OfficeVersions
        (Id, OfficeId, ClassificationCode, Name, EffectiveFrom, EffectiveToExclusive,
         IsActive, RevisionNumber, ChangeReason, DecisionReference, CreatedAtUtc, CreatedByUserId,
         SupersededAtUtc, SupersededByUserId, SupersededReason)
    SELECT NEWID(), o.Id, m.ClassificationCode, m.Name, @EffectiveFrom, NULL,
           1, 1, N'Initial organization office baseline', N'BRD-HRIS-ORG v1.0',
           SYSUTCDATETIME(), @ActorUserId, NULL, NULL, NULL
    FROM @Master m INNER JOIN dbo.Offices o ON o.OfficeCode = m.OfficeCode
    WHERE NOT EXISTS (SELECT 1 FROM dbo.OfficeVersions v WITH (UPDLOCK, HOLDLOCK) WHERE v.OfficeId = o.Id);
    SET @InsertedVersions = @@ROWCOUNT;

    -- These values are validated GUID/date/integer types, never arbitrary text.
    -- Compose the fixed audit object without requiring SQL Server JSON-generation
    -- syntax. Free-text fields must not be added here without JSON escaping.
    DECLARE @DetailJson nvarchar(4000);
    SET @DetailJson = N'{"actorUserId":"' + CONVERT(nvarchar(36), @ActorUserId)
        + N'","effectiveFrom":"' + CONVERT(nvarchar(10), @EffectiveFrom, 23)
        + N'","insertedClassifications":' + CONVERT(nvarchar(11), @InsertedClassifications)
        + N',"insertedOffices":' + CONVERT(nvarchar(11), @InsertedOffices)
        + N',"insertedVersions":' + CONVERT(nvarchar(11), @InsertedVersions)
        + N',"rolesChanged":false}';

    INSERT INTO dbo.AuditLogs
        (Id, TimestampUtc, UserId, ClientAppId, EventType, IsSuccess, IpAddress, UserAgent, DetailJson)
    VALUES (NEWID(), SYSUTCDATETIME(), @ActorUserId, @AdminAppId,
            N'OrganizationMasterProvisioned', 1, NULL, N'operator-sqlcmd-provisioning', @DetailJson);

    COMMIT TRANSACTION;
    SELECT @InsertedClassifications AS InsertedClassifications,
           @InsertedOffices AS InsertedOffices, @InsertedVersions AS InsertedVersions;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
