using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LuminaPath.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DocumentStorageName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "StorageName",
                table: "Documents",
                type: "text",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE "Documents"
                SET "StorageName" = "Name"
                WHERE "StorageName" IS NULL
                  AND "Path" IS NULL
                  AND "Name" IS NOT NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StorageName",
                table: "Documents");
        }
    }
}
