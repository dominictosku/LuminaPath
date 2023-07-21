using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Data.Migrations
{
    /// <inheritdoc />
    public partial class LuminaUserRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LuminaUserId",
                table: "PersonalGaming",
                type: "varchar(255)",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "ReleaseDate",
                table: "Games",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PersonalGaming_LuminaUserId",
                table: "PersonalGaming",
                column: "LuminaUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_PersonalGaming_AspNetUsers_LuminaUserId",
                table: "PersonalGaming",
                column: "LuminaUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PersonalGaming_AspNetUsers_LuminaUserId",
                table: "PersonalGaming");

            migrationBuilder.DropIndex(
                name: "IX_PersonalGaming_LuminaUserId",
                table: "PersonalGaming");

            migrationBuilder.DropColumn(
                name: "LuminaUserId",
                table: "PersonalGaming");

            migrationBuilder.DropColumn(
                name: "ReleaseDate",
                table: "Games");
        }
    }
}
