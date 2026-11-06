using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AuthApi.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEmployeeProfileFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Employees",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    EmployeeFullName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Employees", x => x.Id);
                    table.CheckConstraint("CK_Employees_EmployeeFullName_NotBlank", "LEN(LTRIM(RTRIM([EmployeeFullName]))) > 0");
                    table.CheckConstraint("CK_Employees_EmployeeNumber_NotBlank", "LEN(LTRIM(RTRIM([EmployeeNumber]))) > 0");
                    table.ForeignKey(
                        name: "FK_Employees_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Employees_Users_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EmployeePositions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssignmentKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PositionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    EffectiveToExclusive = table.Column<DateOnly>(type: "date", nullable: true),
                    RecordState = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
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
                    table.PrimaryKey("PK_EmployeePositions", x => x.Id);
                    table.UniqueConstraint("AK_EmployeePositions_Employee_Id", x => new { x.EmployeeId, x.Id });
                    table.CheckConstraint("CK_EmployeePositions_ChangeReason_NotBlank", "LEN(LTRIM(RTRIM([ChangeReason]))) > 0");
                    table.CheckConstraint("CK_EmployeePositions_EffectiveRange", "[EffectiveToExclusive] IS NULL OR [EffectiveToExclusive] > [EffectiveFrom]");
                    table.CheckConstraint("CK_EmployeePositions_RecordState", "[RecordState] IN ('DRAFT', 'CONFIRMED', 'CANCELLED')");
                    table.CheckConstraint("CK_EmployeePositions_RevisionNumber", "[RevisionNumber] > 0");
                    table.CheckConstraint("CK_EmployeePositions_SupersededMetadata", "([SupersededAtUtc] IS NULL AND [SupersededByUserId] IS NULL AND [SupersededReason] IS NULL) OR ([SupersededAtUtc] IS NOT NULL AND [SupersededByUserId] IS NOT NULL AND [SupersededReason] IS NOT NULL AND LEN(LTRIM(RTRIM([SupersededReason]))) > 0)");
                    table.ForeignKey(
                        name: "FK_EmployeePositions_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmployeePositions_Positions_PositionId",
                        column: x => x.PositionId,
                        principalTable: "Positions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmployeePositions_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmployeePositions_Users_SupersededByUserId",
                        column: x => x.SupersededByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeStatuses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StatusCode = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    EffectiveToExclusive = table.Column<DateOnly>(type: "date", nullable: true),
                    RecordState = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
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
                    table.PrimaryKey("PK_EmployeeStatuses", x => x.Id);
                    table.CheckConstraint("CK_EmployeeStatuses_ChangeReason_NotBlank", "LEN(LTRIM(RTRIM([ChangeReason]))) > 0");
                    table.CheckConstraint("CK_EmployeeStatuses_EffectiveRange", "[EffectiveToExclusive] IS NULL OR [EffectiveToExclusive] > [EffectiveFrom]");
                    table.CheckConstraint("CK_EmployeeStatuses_RecordState", "[RecordState] IN ('DRAFT', 'CONFIRMED', 'CANCELLED')");
                    table.CheckConstraint("CK_EmployeeStatuses_RevisionNumber", "[RevisionNumber] > 0");
                    table.CheckConstraint("CK_EmployeeStatuses_StatusCode", "[StatusCode] IN ('ACTIVE', 'RESIGNED')");
                    table.CheckConstraint("CK_EmployeeStatuses_SupersededMetadata", "([SupersededAtUtc] IS NULL AND [SupersededByUserId] IS NULL AND [SupersededReason] IS NULL) OR ([SupersededAtUtc] IS NOT NULL AND [SupersededByUserId] IS NOT NULL AND [SupersededReason] IS NOT NULL AND LEN(LTRIM(RTRIM([SupersededReason]))) > 0)");
                    table.ForeignKey(
                        name: "FK_EmployeeStatuses_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmployeeStatuses_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmployeeStatuses_Users_SupersededByUserId",
                        column: x => x.SupersededByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserEmployeeLinks",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LinkedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LinkedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserEmployeeLinks", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_UserEmployeeLinks_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserEmployeeLinks_Users_LinkedByUserId",
                        column: x => x.LinkedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserEmployeeLinks_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeActingAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssignmentKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceEmployeePositionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TargetPositionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssignmentType = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    EffectiveToExclusive = table.Column<DateOnly>(type: "date", nullable: true),
                    RecordState = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    RevisionNumber = table.Column<int>(type: "int", nullable: false),
                    ChangeReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    DecisionReference = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SupersededAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SupersededByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SupersededReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeActingAssignments", x => x.Id);
                    table.CheckConstraint("CK_EmployeeActingAssignments_AssignmentType", "[AssignmentType] IN ('PLH', 'PLT')");
                    table.CheckConstraint("CK_EmployeeActingAssignments_ChangeReason_NotBlank", "LEN(LTRIM(RTRIM([ChangeReason]))) > 0");
                    table.CheckConstraint("CK_EmployeeActingAssignments_DecisionReference_NotBlank", "LEN(LTRIM(RTRIM([DecisionReference]))) > 0");
                    table.CheckConstraint("CK_EmployeeActingAssignments_EffectiveRange", "[EffectiveToExclusive] IS NULL OR [EffectiveToExclusive] > [EffectiveFrom]");
                    table.CheckConstraint("CK_EmployeeActingAssignments_RecordState", "[RecordState] IN ('DRAFT', 'CONFIRMED', 'CANCELLED')");
                    table.CheckConstraint("CK_EmployeeActingAssignments_RevisionNumber", "[RevisionNumber] > 0");
                    table.CheckConstraint("CK_EmployeeActingAssignments_SupersededMetadata", "([SupersededAtUtc] IS NULL AND [SupersededByUserId] IS NULL AND [SupersededReason] IS NULL) OR ([SupersededAtUtc] IS NOT NULL AND [SupersededByUserId] IS NOT NULL AND [SupersededReason] IS NOT NULL AND LEN(LTRIM(RTRIM([SupersededReason]))) > 0)");
                    table.ForeignKey(
                        name: "FK_EmployeeActingAssignments_EmployeePositions_EmployeeId_SourceEmployeePositionId",
                        columns: x => new { x.EmployeeId, x.SourceEmployeePositionId },
                        principalTable: "EmployeePositions",
                        principalColumns: new[] { "EmployeeId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmployeeActingAssignments_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmployeeActingAssignments_Positions_TargetPositionId",
                        column: x => x.TargetPositionId,
                        principalTable: "Positions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmployeeActingAssignments_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmployeeActingAssignments_Users_SupersededByUserId",
                        column: x => x.SupersededByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeActingAssignments_CreatedByUserId",
                table: "EmployeeActingAssignments",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeActingAssignments_Employee_EffectiveRange",
                table: "EmployeeActingAssignments",
                columns: new[] { "EmployeeId", "EffectiveFrom", "EffectiveToExclusive" });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeActingAssignments_EmployeeId_SourceEmployeePositionId",
                table: "EmployeeActingAssignments",
                columns: new[] { "EmployeeId", "SourceEmployeePositionId" });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeActingAssignments_SourcePosition_EffectiveRange",
                table: "EmployeeActingAssignments",
                columns: new[] { "SourceEmployeePositionId", "EffectiveFrom", "EffectiveToExclusive" });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeActingAssignments_SupersededByUserId",
                table: "EmployeeActingAssignments",
                column: "SupersededByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeActingAssignments_TargetPosition_EffectiveRange",
                table: "EmployeeActingAssignments",
                columns: new[] { "TargetPositionId", "EffectiveFrom", "EffectiveToExclusive" });

            migrationBuilder.CreateIndex(
                name: "UX_EmployeeActingAssignments_Assignment_CurrentRevision",
                table: "EmployeeActingAssignments",
                column: "AssignmentKey",
                unique: true,
                filter: "[SupersededAtUtc] IS NULL");

            migrationBuilder.CreateIndex(
                name: "UX_EmployeeActingAssignments_Assignment_Revision",
                table: "EmployeeActingAssignments",
                columns: new[] { "AssignmentKey", "RevisionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmployeePositions_CreatedByUserId",
                table: "EmployeePositions",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeePositions_Employee_EffectiveRange",
                table: "EmployeePositions",
                columns: new[] { "EmployeeId", "EffectiveFrom", "EffectiveToExclusive" });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeePositions_Position_EffectiveRange",
                table: "EmployeePositions",
                columns: new[] { "PositionId", "EffectiveFrom", "EffectiveToExclusive" });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeePositions_SupersededByUserId",
                table: "EmployeePositions",
                column: "SupersededByUserId");

            migrationBuilder.CreateIndex(
                name: "UX_EmployeePositions_Assignment_CurrentRevision",
                table: "EmployeePositions",
                column: "AssignmentKey",
                unique: true,
                filter: "[SupersededAtUtc] IS NULL");

            migrationBuilder.CreateIndex(
                name: "UX_EmployeePositions_Assignment_Revision",
                table: "EmployeePositions",
                columns: new[] { "AssignmentKey", "RevisionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Employees_CreatedByUserId",
                table: "Employees",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_UpdatedByUserId",
                table: "Employees",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "UX_Employees_EmployeeNumber",
                table: "Employees",
                column: "EmployeeNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeStatuses_CreatedByUserId",
                table: "EmployeeStatuses",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeStatuses_Employee_EffectiveRange",
                table: "EmployeeStatuses",
                columns: new[] { "EmployeeId", "EffectiveFrom", "EffectiveToExclusive" });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeStatuses_SupersededByUserId",
                table: "EmployeeStatuses",
                column: "SupersededByUserId");

            migrationBuilder.CreateIndex(
                name: "UX_EmployeeStatuses_Employee_Revision",
                table: "EmployeeStatuses",
                columns: new[] { "EmployeeId", "RevisionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserEmployeeLinks_LinkedByUserId",
                table: "UserEmployeeLinks",
                column: "LinkedByUserId");

            migrationBuilder.CreateIndex(
                name: "UX_UserEmployeeLinks_EmployeeId",
                table: "UserEmployeeLinks",
                column: "EmployeeId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmployeeActingAssignments");

            migrationBuilder.DropTable(
                name: "EmployeeStatuses");

            migrationBuilder.DropTable(
                name: "UserEmployeeLinks");

            migrationBuilder.DropTable(
                name: "EmployeePositions");

            migrationBuilder.DropTable(
                name: "Employees");
        }
    }
}
