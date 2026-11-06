# Generates a reviewable SQL file only. Never connects to or runs SQL Server.
[CmdletBinding()]
param(
    [ValidateSet(1, 2)][int]$Version = 1,
    [string]$CatalogPath,
    [string]$OutputPath
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$scriptDirectory = Split-Path -Parent $MyInvocation.MyCommand.Path
if ([string]::IsNullOrWhiteSpace($CatalogPath)) {
    $CatalogPath = Join-Path $scriptDirectory ("../data/organization/operational-templates.v$Version.json")
}
if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $outputName = if ($Version -eq 1) { 'provision-organization-templates.sql' } else { "provision-organization-templates-v$Version.sql" }
    $OutputPath = Join-Path $scriptDirectory "database/$outputName"
}

function ConvertTo-SqlLiteral([AllowNull()][object]$Value) {
    if ($null -eq $Value) { return 'NULL' }
    return "N'" + ([string]$Value).Replace("'", "''") + "'"
}

function Assert-CatalogText([object]$Value, [int]$MaximumLength) {
    if ([string]::IsNullOrWhiteSpace([string]$Value) -or ([string]$Value).Length -gt $MaximumLength -or
        ([string]$Value) -match '[\x00-\x1F]' -or ([string]$Value).Contains('$(')) {
        throw 'Catalog text is missing, too long, contains control characters or SQLCMD substitution.'
    }
}

$catalog = Get-Content -LiteralPath $CatalogPath -Raw -Encoding UTF8 | ConvertFrom-Json
if ($catalog.schemaVersion -ne 1 -or $catalog.state -cne 'DRAFT') { throw 'Only schema v1 DRAFT catalogs are supported.' }
$classifications = @($catalog.templates | ForEach-Object { $_.classificationCode })
if ($classifications.Count -ne 5 -or (Compare-Object @('CAB-I','CAB-II','CAB-III','PEMASAR','HEAD_OFFICE') $classifications)) {
    throw 'Expected exactly the five approved classifications (four operational plus head office).'
}
Assert-CatalogText $catalog.notes 1000
$levels = @($catalog.levels)
if ($levels.Count -lt 1) { throw 'Catalog must define at least one position level.' }
$levelCodes = @($levels | ForEach-Object { $_.code })
$levelOrders = @($levels | ForEach-Object { $_.displayOrder })
if (@($levelCodes | Sort-Object -Unique).Count -ne $levelCodes.Count -or
    @($levelOrders | Sort-Object -Unique).Count -ne $levelOrders.Count) { throw 'Duplicate level codes or display orders.' }
$levelRows = [System.Collections.Generic.List[string]]::new()
foreach ($level in $levels) {
    if ($level.code -cnotmatch '^[A-Z][A-Z0-9_]{0,49}$') { throw 'Invalid level code.' }
    Assert-CatalogText $level.name 150
    if ($level.displayOrder -isnot [int] -or $level.displayOrder -lt 0) { throw 'Invalid level display order.' }
    $levelRows.Add('(' + (ConvertTo-SqlLiteral $level.code) + ', ' + (ConvertTo-SqlLiteral $level.name) + ', ' + $level.displayOrder + ')')
}
$unitRows = [System.Collections.Generic.List[string]]::new()
$positionRows = [System.Collections.Generic.List[string]]::new()
foreach ($template in $catalog.templates) {
    if ($template.version -ne $Version) { throw 'Template version does not match the requested catalog version.' }
    $unitCodes = @($template.units | ForEach-Object { $_.code })
    $positionCodes = @($template.positions | ForEach-Object { $_.code })
    if (@($unitCodes | Sort-Object -Unique).Count -ne $unitCodes.Count -or
        @($positionCodes | Sort-Object -Unique).Count -ne $positionCodes.Count) { throw 'Duplicate catalog codes.' }
    foreach ($unit in $template.units) {
        if ($unit.code -cnotmatch '^[A-Z][A-Z0-9_]{0,49}$' -or $unit.type -cnotmatch '^(OTHER|SECTION|DIRECTORATE|DIVISION|DEPARTMENT)$') { throw 'Invalid unit code/type.' }
        Assert-CatalogText $unit.name 200
        if ($null -ne $unit.parentCode -and ($unit.parentCode -notin $unitCodes -or $unit.parentCode -eq $unit.code)) { throw 'Invalid unit parent.' }
        $values = @($template.classificationCode, $unit.code, $unit.parentCode, $unit.name, $unit.type) | ForEach-Object { ConvertTo-SqlLiteral $_ }
        $unitRows.Add('(' + ($values -join ', ') + ')')
    }
    foreach ($position in $template.positions) {
        if ($position.code -cnotmatch '^[A-Z][A-Z0-9_]{0,49}$' -or $position.unitCode -notin $unitCodes -or
            $position.track -cne 'LINE' -or ($null -ne $position.levelCode -and $position.levelCode -notin $levelCodes)) { throw 'Invalid position code, unit, track or unknown level.' }
        Assert-CatalogText $position.name 200
        if ($null -ne $position.reportsToCode -and ($position.reportsToCode -notin $positionCodes -or $position.reportsToCode -eq $position.code)) { throw 'Invalid reporting position.' }
        if (($position.isUnitHead -and $position.maxIncumbents -ne 1) -or
            (-not $position.isUnitHead -and $null -ne $position.maxIncumbents)) { throw 'Invalid approved capacity.' }
        $values = @($template.classificationCode, $position.code, $position.unitCode, $position.reportsToCode, $position.name, $position.track) | ForEach-Object { ConvertTo-SqlLiteral $_ }
        $head = if ($position.isUnitHead) { '1' } else { '0' }
        $capacity = if ($null -eq $position.maxIncumbents) { 'NULL' } else { '1' }
        $positionRows.Add('(' + ($values -join ', ') + ', ' + $head + ', ' + $capacity + ', ' + (ConvertTo-SqlLiteral $position.levelCode) + ')')
    }
}

$body = @'
-- GENERATED from data/organization/operational-templates.v__VERSION__.json.
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
__LEVEL_ROWS__;
INSERT INTO @Units (ClassificationCode, UnitCode, ParentCode, Name, UnitType) VALUES
__UNIT_ROWS__;
INSERT INTO @Positions (ClassificationCode, PositionCode, UnitCode, SupervisorCode, Name, Track, IsUnitHead, MaxIncumbents, LevelCode) VALUES
__POSITION_ROWS__;

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
        ON t.ClassificationCode = c.ClassificationCode AND t.Version = __VERSION__;

    IF EXISTS (
        SELECT 1 FROM @Templates e JOIN dbo.StructureTemplates t WITH (UPDLOCK, HOLDLOCK)
            ON t.ClassificationCode = e.ClassificationCode
        WHERE e.IsNew = 1 AND t.Version __NEWER_VERSION_PREDICATE__
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
    SELECT TemplateId, ClassificationCode, __VERSION__, 'DRAFT', __CATALOG_NOTES__, SYSUTCDATETIME(), @ActorUserId, NULL, NULL
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
        + N'","templateVersion":__VERSION__,"insertedLevels":' + CONVERT(nvarchar(11), @InsertedLevels)
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
'@

$body = $body.Replace('__NEWER_VERSION_PREDICATE__', $(if ($Version -eq 1) { '<> 1' } else { "> $Version" }))
$body = $body.Replace('__VERSION__', [string]$Version)
$body = $body.Replace('__LEVEL_ROWS__', ($levelRows -join ",`r`n"))
$body = $body.Replace('__UNIT_ROWS__', ($unitRows -join ",`r`n"))
$body = $body.Replace('__POSITION_ROWS__', ($positionRows -join ",`r`n"))
$body = $body.Replace('__CATALOG_NOTES__', (ConvertTo-SqlLiteral $catalog.notes))
$resolvedOutput = [System.IO.Path]::GetFullPath($OutputPath)
[System.IO.File]::WriteAllText($resolvedOutput, $body + [Environment]::NewLine, [System.Text.UTF8Encoding]::new($true))
Write-Output ('Generated SQL file only: {0} levels, {1} templates, {2} units, {3} positions.' -f $levelRows.Count, $classifications.Count, $unitRows.Count, $positionRows.Count)
