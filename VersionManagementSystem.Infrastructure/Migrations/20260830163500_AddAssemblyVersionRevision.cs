using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VersionManagementSystem.Infrastructure.Migrations
{
    /// <summary>Adds the fourth .NET AssemblyVersion component (Revision).</summary>
    public partial class AddAssemblyVersionRevision : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Revision",
                table: "ApplicationVersions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql(
                "UPDATE ApplicationVersions SET MinimumSupportedVersion = CONCAT(MinimumSupportedVersion, '.0') " +
                "WHERE MinimumSupportedVersion IS NOT NULL AND MinimumSupportedVersion REGEXP '^[0-9]+\\.[0-9]+\\.[0-9]+$';");

            migrationBuilder.DropIndex(
                name: "IX_ApplicationVersions_ApplicationId_Major_Minor_Patch",
                table: "ApplicationVersions");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationVersions_ApplicationId_Major_Minor_Patch_Revision",
                table: "ApplicationVersions",
                columns: new[] { "ApplicationId", "Major", "Minor", "Patch", "Revision" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ApplicationVersions_ApplicationId_Major_Minor_Patch_Revision",
                table: "ApplicationVersions");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationVersions_ApplicationId_Major_Minor_Patch",
                table: "ApplicationVersions",
                columns: new[] { "ApplicationId", "Major", "Minor", "Patch" },
                unique: true);

            migrationBuilder.DropColumn(
                name: "Revision",
                table: "ApplicationVersions");
        }
    }
}
