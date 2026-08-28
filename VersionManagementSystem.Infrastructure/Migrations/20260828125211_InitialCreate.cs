using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VersionManagementSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Applications",
                columns: table => new
                {
                    ApplicationId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ApplicationCode = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ApplicationName = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Applications", x => x.ApplicationId);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ApplicationVersions",
                columns: table => new
                {
                    ApplicationVersionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ApplicationId = table.Column<int>(type: "int", nullable: false),
                    Major = table.Column<int>(type: "int", nullable: false),
                    Minor = table.Column<int>(type: "int", nullable: false),
                    Patch = table.Column<int>(type: "int", nullable: false),
                    ReleaseType = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ReleaseStatus = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ReleaseDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ReleaseNotes = table.Column<string>(type: "varchar(4000)", maxLength: 4000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MinimumSupportedVersion = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsMandatory = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Channel = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedBy = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PublishedDate = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    PublishedBy = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationVersions", x => x.ApplicationVersionId);
                    table.ForeignKey(
                        name: "FK_ApplicationVersions_Applications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "Applications",
                        principalColumn: "ApplicationId",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ClientInstallations",
                columns: table => new
                {
                    ClientInstallationId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ApplicationId = table.Column<int>(type: "int", nullable: false),
                    MachineName = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CurrentVersion = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LastChecked = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientInstallations", x => x.ClientInstallationId);
                    table.ForeignKey(
                        name: "FK_ClientInstallations_Applications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "Applications",
                        principalColumn: "ApplicationId",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "UpdateHistories",
                columns: table => new
                {
                    UpdateHistoryId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ApplicationId = table.Column<int>(type: "int", nullable: false),
                    FromVersion = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ToVersion = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UpdateDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Status = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MachineName = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UpdateHistories", x => x.UpdateHistoryId);
                    table.ForeignKey(
                        name: "FK_UpdateHistories_Applications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "Applications",
                        principalColumn: "ApplicationId",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ReleaseNotes",
                columns: table => new
                {
                    ReleaseNoteId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ApplicationVersionId = table.Column<int>(type: "int", nullable: false),
                    Category = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReleaseNotes", x => x.ReleaseNoteId);
                    table.ForeignKey(
                        name: "FK_ReleaseNotes_ApplicationVersions_ApplicationVersionId",
                        column: x => x.ApplicationVersionId,
                        principalTable: "ApplicationVersions",
                        principalColumn: "ApplicationVersionId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "UpdatePackages",
                columns: table => new
                {
                    UpdatePackageId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ApplicationVersionId = table.Column<int>(type: "int", nullable: false),
                    FileName = table.Column<string>(type: "varchar(260)", maxLength: 260, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FilePath = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    Checksum = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PackageType = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UploadedBy = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UpdatePackages", x => x.UpdatePackageId);
                    table.ForeignKey(
                        name: "FK_UpdatePackages_ApplicationVersions_ApplicationVersionId",
                        column: x => x.ApplicationVersionId,
                        principalTable: "ApplicationVersions",
                        principalColumn: "ApplicationVersionId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Applications_ApplicationCode",
                table: "Applications",
                column: "ApplicationCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationVersions_ApplicationId_Major_Minor_Patch",
                table: "ApplicationVersions",
                columns: new[] { "ApplicationId", "Major", "Minor", "Patch" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClientInstallations_ApplicationId_MachineName",
                table: "ClientInstallations",
                columns: new[] { "ApplicationId", "MachineName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReleaseNotes_ApplicationVersionId",
                table: "ReleaseNotes",
                column: "ApplicationVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_UpdateHistories_ApplicationId_UpdateDate",
                table: "UpdateHistories",
                columns: new[] { "ApplicationId", "UpdateDate" });

            migrationBuilder.CreateIndex(
                name: "IX_UpdatePackages_ApplicationVersionId_PackageType",
                table: "UpdatePackages",
                columns: new[] { "ApplicationVersionId", "PackageType" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClientInstallations");

            migrationBuilder.DropTable(
                name: "ReleaseNotes");

            migrationBuilder.DropTable(
                name: "UpdateHistories");

            migrationBuilder.DropTable(
                name: "UpdatePackages");

            migrationBuilder.DropTable(
                name: "ApplicationVersions");

            migrationBuilder.DropTable(
                name: "Applications");
        }
    }
}
