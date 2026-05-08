using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LuminaPath.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSteamProfileToUserInfo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SteamAvatarUrl",
                table: "LuminaUserInfo",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SteamId",
                table: "LuminaUserInfo",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SteamPersonaName",
                table: "LuminaUserInfo",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SteamProfileUrl",
                table: "LuminaUserInfo",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SteamAvatarUrl",
                table: "LuminaUserInfo");

            migrationBuilder.DropColumn(
                name: "SteamId",
                table: "LuminaUserInfo");

            migrationBuilder.DropColumn(
                name: "SteamPersonaName",
                table: "LuminaUserInfo");

            migrationBuilder.DropColumn(
                name: "SteamProfileUrl",
                table: "LuminaUserInfo");
        }
    }
}
