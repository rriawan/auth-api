using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AuthApi.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizationMaster : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OfficeClassifications",
                columns: table => new
                {
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OfficeClassifications", x => x.Code);
                });

            migrationBuilder.CreateTable(
                name: "Offices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OfficeCode = table.Column<string>(type: "varchar(3)", unicode: false, maxLength: 3, nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Offices", x => x.Id);
                    table.CheckConstraint("CK_Offices_OfficeCode_ThreeDigits", "DATALENGTH([OfficeCode]) = 3 AND [OfficeCode] COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^0-9]%'");
                    table.ForeignKey(
                        name: "FK_Offices_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PositionLevels",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PositionLevels", x => x.Id);
                    table.CheckConstraint("CK_PositionLevels_DisplayOrder", "[DisplayOrder] >= 0");
                    table.ForeignKey(
                        name: "FK_PositionLevels_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StructureTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClassificationCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    State = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PublishedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PublishedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StructureTemplates", x => x.Id);
                    table.CheckConstraint("CK_StructureTemplates_PublicationMetadata", "([State] = 'DRAFT' AND [PublishedAtUtc] IS NULL AND [PublishedByUserId] IS NULL) OR ([State] = 'PUBLISHED' AND [PublishedAtUtc] IS NOT NULL AND [PublishedByUserId] IS NOT NULL)");
                    table.CheckConstraint("CK_StructureTemplates_Version", "[Version] > 0");
                    table.ForeignKey(
                        name: "FK_StructureTemplates_OfficeClassifications_ClassificationCode",
                        column: x => x.ClassificationCode,
                        principalTable: "OfficeClassifications",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StructureTemplates_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StructureTemplates_Users_PublishedByUserId",
                        column: x => x.PublishedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OfficeVersions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OfficeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClassificationCode = table.Column<string>(type: "nvarchar(20)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    EffectiveToExclusive = table.Column<DateOnly>(type: "date", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    RevisionNumber = table.Column<int>(type: "int", nullable: false),
                    ChangeReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    DecisionReference = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SupersededAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SupersededByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SupersededReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OfficeVersions", x => x.Id);
                    table.CheckConstraint("CK_OfficeVersions_EffectiveRange", "[EffectiveToExclusive] IS NULL OR [EffectiveToExclusive] > [EffectiveFrom]");
                    table.CheckConstraint("CK_OfficeVersions_RevisionNumber", "[RevisionNumber] > 0");
                    table.CheckConstraint("CK_OfficeVersions_SupersededMetadata", "(([SupersededAtUtc] IS NULL AND [SupersededByUserId] IS NULL AND [SupersededReason] IS NULL) OR ([SupersededAtUtc] IS NOT NULL AND [SupersededByUserId] IS NOT NULL AND [SupersededReason] IS NOT NULL AND LEN(LTRIM(RTRIM([SupersededReason]))) > 0))");
                    table.ForeignKey(
                        name: "FK_OfficeVersions_OfficeClassifications_ClassificationCode",
                        column: x => x.ClassificationCode,
                        principalTable: "OfficeClassifications",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OfficeVersions_Offices_OfficeId",
                        column: x => x.OfficeId,
                        principalTable: "Offices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OfficeVersions_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OfficeVersions_Users_SupersededByUserId",
                        column: x => x.SupersededByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OrgUnits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OfficeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UnitCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrgUnits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrgUnits_Offices_OfficeId",
                        column: x => x.OfficeId,
                        principalTable: "Offices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrgUnits_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OfficeTemplateApplications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OfficeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    RequestHash = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    AppliedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AppliedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OfficeTemplateApplications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OfficeTemplateApplications_Offices_OfficeId",
                        column: x => x.OfficeId,
                        principalTable: "Offices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OfficeTemplateApplications_StructureTemplates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "StructureTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OfficeTemplateApplications_Users_AppliedByUserId",
                        column: x => x.AppliedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TemplateOrgUnits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParentTemplateOrgUnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UnitCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    UnitType = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TemplateOrgUnits", x => x.Id);
                    table.UniqueConstraint("AK_TemplateOrgUnits_Template_Id", x => new { x.TemplateId, x.Id });
                    table.CheckConstraint("CK_TemplateOrgUnits_NotSelfParent", "[ParentTemplateOrgUnitId] IS NULL OR [ParentTemplateOrgUnitId] <> [Id]");
                    table.CheckConstraint("CK_TemplateOrgUnits_UnitType", "[UnitType] IN ('DIRECTORATE', 'DIVISION', 'DEPARTMENT', 'SECTION', 'WORKGROUP', 'OTHER')");
                    table.ForeignKey(
                        name: "FK_TemplateOrgUnits_StructureTemplates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "StructureTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TemplateOrgUnits_TemplateOrgUnits_TemplateId_ParentTemplateOrgUnitId",
                        columns: x => new { x.TemplateId, x.ParentTemplateOrgUnitId },
                        principalTable: "TemplateOrgUnits",
                        principalColumns: new[] { "TemplateId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OrgUnitVersions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrgUnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParentOrgUnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UnitType = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    EffectiveToExclusive = table.Column<DateOnly>(type: "date", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    RevisionNumber = table.Column<int>(type: "int", nullable: false),
                    ChangeReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    DecisionReference = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SupersededAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SupersededByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SupersededReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrgUnitVersions", x => x.Id);
                    table.CheckConstraint("CK_OrgUnitVersions_EffectiveRange", "[EffectiveToExclusive] IS NULL OR [EffectiveToExclusive] > [EffectiveFrom]");
                    table.CheckConstraint("CK_OrgUnitVersions_NotSelfParent", "[ParentOrgUnitId] IS NULL OR [ParentOrgUnitId] <> [OrgUnitId]");
                    table.CheckConstraint("CK_OrgUnitVersions_RevisionNumber", "[RevisionNumber] > 0");
                    table.CheckConstraint("CK_OrgUnitVersions_SupersededMetadata", "(([SupersededAtUtc] IS NULL AND [SupersededByUserId] IS NULL AND [SupersededReason] IS NULL) OR ([SupersededAtUtc] IS NOT NULL AND [SupersededByUserId] IS NOT NULL AND [SupersededReason] IS NOT NULL AND LEN(LTRIM(RTRIM([SupersededReason]))) > 0))");
                    table.CheckConstraint("CK_OrgUnitVersions_UnitType", "[UnitType] IN ('DIRECTORATE', 'DIVISION', 'DEPARTMENT', 'SECTION', 'WORKGROUP', 'OTHER')");
                    table.ForeignKey(
                        name: "FK_OrgUnitVersions_OrgUnits_OrgUnitId",
                        column: x => x.OrgUnitId,
                        principalTable: "OrgUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrgUnitVersions_OrgUnits_ParentOrgUnitId",
                        column: x => x.ParentOrgUnitId,
                        principalTable: "OrgUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrgUnitVersions_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrgUnitVersions_Users_SupersededByUserId",
                        column: x => x.SupersededByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Positions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrgUnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PositionCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Positions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Positions_OrgUnits_OrgUnitId",
                        column: x => x.OrgUnitId,
                        principalTable: "OrgUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Positions_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TemplatePositions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TemplateOrgUnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReportsToTemplatePositionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LevelId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PositionCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Track = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    IsUnitHead = table.Column<bool>(type: "bit", nullable: false),
                    MaxIncumbents = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TemplatePositions", x => x.Id);
                    table.UniqueConstraint("AK_TemplatePositions_Template_Id", x => new { x.TemplateId, x.Id });
                    table.CheckConstraint("CK_TemplatePositions_MaxIncumbents", "[MaxIncumbents] IS NULL OR [MaxIncumbents] > 0");
                    table.CheckConstraint("CK_TemplatePositions_NotSelfSupervisor", "[ReportsToTemplatePositionId] IS NULL OR [ReportsToTemplatePositionId] <> [Id]");
                    table.CheckConstraint("CK_TemplatePositions_Track", "[Track] IN ('LINE', 'FUNCTIONAL')");
                    table.ForeignKey(
                        name: "FK_TemplatePositions_PositionLevels_LevelId",
                        column: x => x.LevelId,
                        principalTable: "PositionLevels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TemplatePositions_StructureTemplates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "StructureTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TemplatePositions_TemplateOrgUnits_TemplateId_TemplateOrgUnitId",
                        columns: x => new { x.TemplateId, x.TemplateOrgUnitId },
                        principalTable: "TemplateOrgUnits",
                        principalColumns: new[] { "TemplateId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TemplatePositions_TemplatePositions_TemplateId_ReportsToTemplatePositionId",
                        columns: x => new { x.TemplateId, x.ReportsToTemplatePositionId },
                        principalTable: "TemplatePositions",
                        principalColumns: new[] { "TemplateId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PositionVersions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PositionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LevelId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReportsToPositionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Track = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    IsUnitHead = table.Column<bool>(type: "bit", nullable: false),
                    MaxIncumbents = table.Column<int>(type: "int", nullable: true),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    EffectiveToExclusive = table.Column<DateOnly>(type: "date", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    RevisionNumber = table.Column<int>(type: "int", nullable: false),
                    ChangeReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    DecisionReference = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SupersededAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SupersededByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SupersededReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PositionVersions", x => x.Id);
                    table.CheckConstraint("CK_PositionVersions_EffectiveRange", "[EffectiveToExclusive] IS NULL OR [EffectiveToExclusive] > [EffectiveFrom]");
                    table.CheckConstraint("CK_PositionVersions_MaxIncumbents", "[MaxIncumbents] IS NULL OR [MaxIncumbents] > 0");
                    table.CheckConstraint("CK_PositionVersions_NotSelfSupervisor", "[ReportsToPositionId] IS NULL OR [ReportsToPositionId] <> [PositionId]");
                    table.CheckConstraint("CK_PositionVersions_RevisionNumber", "[RevisionNumber] > 0");
                    table.CheckConstraint("CK_PositionVersions_SupersededMetadata", "(([SupersededAtUtc] IS NULL AND [SupersededByUserId] IS NULL AND [SupersededReason] IS NULL) OR ([SupersededAtUtc] IS NOT NULL AND [SupersededByUserId] IS NOT NULL AND [SupersededReason] IS NOT NULL AND LEN(LTRIM(RTRIM([SupersededReason]))) > 0))");
                    table.CheckConstraint("CK_PositionVersions_Track", "[Track] IN ('LINE', 'FUNCTIONAL')");
                    table.ForeignKey(
                        name: "FK_PositionVersions_PositionLevels_LevelId",
                        column: x => x.LevelId,
                        principalTable: "PositionLevels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PositionVersions_Positions_PositionId",
                        column: x => x.PositionId,
                        principalTable: "Positions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PositionVersions_Positions_ReportsToPositionId",
                        column: x => x.ReportsToPositionId,
                        principalTable: "Positions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PositionVersions_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PositionVersions_Users_SupersededByUserId",
                        column: x => x.SupersededByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Offices_CreatedByUserId",
                table: "Offices",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "UX_Offices_OfficeCode",
                table: "Offices",
                column: "OfficeCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OfficeTemplateApplications_AppliedByUserId",
                table: "OfficeTemplateApplications",
                column: "AppliedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_OfficeTemplateApplications_Office_EffectiveFrom",
                table: "OfficeTemplateApplications",
                columns: new[] { "OfficeId", "EffectiveFrom" });

            migrationBuilder.CreateIndex(
                name: "IX_OfficeTemplateApplications_TemplateId",
                table: "OfficeTemplateApplications",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "UX_OfficeTemplateApplications_Office_IdempotencyKey",
                table: "OfficeTemplateApplications",
                columns: new[] { "OfficeId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OfficeVersions_ClassificationCode",
                table: "OfficeVersions",
                column: "ClassificationCode");

            migrationBuilder.CreateIndex(
                name: "IX_OfficeVersions_CreatedByUserId",
                table: "OfficeVersions",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_OfficeVersions_Office_EffectiveRange",
                table: "OfficeVersions",
                columns: new[] { "OfficeId", "EffectiveFrom", "EffectiveToExclusive" });

            migrationBuilder.CreateIndex(
                name: "IX_OfficeVersions_SupersededByUserId",
                table: "OfficeVersions",
                column: "SupersededByUserId");

            migrationBuilder.CreateIndex(
                name: "UX_OfficeVersions_Office_Revision",
                table: "OfficeVersions",
                columns: new[] { "OfficeId", "RevisionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrgUnits_CreatedByUserId",
                table: "OrgUnits",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "UX_OrgUnits_Office_UnitCode",
                table: "OrgUnits",
                columns: new[] { "OfficeId", "UnitCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrgUnitVersions_CreatedByUserId",
                table: "OrgUnitVersions",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_OrgUnitVersions_ParentOrgUnitId",
                table: "OrgUnitVersions",
                column: "ParentOrgUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_OrgUnitVersions_SupersededByUserId",
                table: "OrgUnitVersions",
                column: "SupersededByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_OrgUnitVersions_Unit_EffectiveRange",
                table: "OrgUnitVersions",
                columns: new[] { "OrgUnitId", "EffectiveFrom", "EffectiveToExclusive" });

            migrationBuilder.CreateIndex(
                name: "UX_OrgUnitVersions_Unit_Revision",
                table: "OrgUnitVersions",
                columns: new[] { "OrgUnitId", "RevisionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PositionLevels_CreatedByUserId",
                table: "PositionLevels",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PositionLevels_DisplayOrder",
                table: "PositionLevels",
                column: "DisplayOrder");

            migrationBuilder.CreateIndex(
                name: "UX_PositionLevels_Code",
                table: "PositionLevels",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Positions_CreatedByUserId",
                table: "Positions",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "UX_Positions_Unit_PositionCode",
                table: "Positions",
                columns: new[] { "OrgUnitId", "PositionCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PositionVersions_CreatedByUserId",
                table: "PositionVersions",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PositionVersions_LevelId",
                table: "PositionVersions",
                column: "LevelId");

            migrationBuilder.CreateIndex(
                name: "IX_PositionVersions_Position_EffectiveRange",
                table: "PositionVersions",
                columns: new[] { "PositionId", "EffectiveFrom", "EffectiveToExclusive" });

            migrationBuilder.CreateIndex(
                name: "IX_PositionVersions_ReportsTo_EffectiveFrom",
                table: "PositionVersions",
                columns: new[] { "ReportsToPositionId", "EffectiveFrom" });

            migrationBuilder.CreateIndex(
                name: "IX_PositionVersions_SupersededByUserId",
                table: "PositionVersions",
                column: "SupersededByUserId");

            migrationBuilder.CreateIndex(
                name: "UX_PositionVersions_Position_Revision",
                table: "PositionVersions",
                columns: new[] { "PositionId", "RevisionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StructureTemplates_CreatedByUserId",
                table: "StructureTemplates",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_StructureTemplates_PublishedByUserId",
                table: "StructureTemplates",
                column: "PublishedByUserId");

            migrationBuilder.CreateIndex(
                name: "UX_StructureTemplates_Classification_Version",
                table: "StructureTemplates",
                columns: new[] { "ClassificationCode", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TemplateOrgUnits_TemplateId_ParentTemplateOrgUnitId",
                table: "TemplateOrgUnits",
                columns: new[] { "TemplateId", "ParentTemplateOrgUnitId" });

            migrationBuilder.CreateIndex(
                name: "UX_TemplateOrgUnits_Template_UnitCode",
                table: "TemplateOrgUnits",
                columns: new[] { "TemplateId", "UnitCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TemplatePositions_LevelId",
                table: "TemplatePositions",
                column: "LevelId");

            migrationBuilder.CreateIndex(
                name: "IX_TemplatePositions_TemplateId_ReportsToTemplatePositionId",
                table: "TemplatePositions",
                columns: new[] { "TemplateId", "ReportsToTemplatePositionId" });

            migrationBuilder.CreateIndex(
                name: "IX_TemplatePositions_TemplateId_TemplateOrgUnitId",
                table: "TemplatePositions",
                columns: new[] { "TemplateId", "TemplateOrgUnitId" });

            migrationBuilder.CreateIndex(
                name: "UX_TemplatePositions_Template_PositionCode",
                table: "TemplatePositions",
                columns: new[] { "TemplateId", "PositionCode" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OfficeTemplateApplications");

            migrationBuilder.DropTable(
                name: "OfficeVersions");

            migrationBuilder.DropTable(
                name: "OrgUnitVersions");

            migrationBuilder.DropTable(
                name: "PositionVersions");

            migrationBuilder.DropTable(
                name: "TemplatePositions");

            migrationBuilder.DropTable(
                name: "Positions");

            migrationBuilder.DropTable(
                name: "PositionLevels");

            migrationBuilder.DropTable(
                name: "TemplateOrgUnits");

            migrationBuilder.DropTable(
                name: "OrgUnits");

            migrationBuilder.DropTable(
                name: "StructureTemplates");

            migrationBuilder.DropTable(
                name: "Offices");

            migrationBuilder.DropTable(
                name: "OfficeClassifications");
        }
    }
}
