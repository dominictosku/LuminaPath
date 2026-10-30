using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LuminaPath.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DocumentStorageMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ChecksumSha256",
                table: "Documents",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "Documents",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<long>(
                name: "FileSizeBytes",
                table: "Documents",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ChecksumSha256",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "FileSizeBytes",
                table: "Documents");
        }
    }
}
