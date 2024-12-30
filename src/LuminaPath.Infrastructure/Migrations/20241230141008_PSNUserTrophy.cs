using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LuminaPath.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PSNUserTrophy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "AccountId",
                table: "LuminaUserInfo",
                newName: "PSNAccountId");

            migrationBuilder.AddColumn<int>(
                name: "PSNBronze",
                table: "LuminaUserInfo",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PSNGold",
                table: "LuminaUserInfo",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PSNPlatinum",
                table: "LuminaUserInfo",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PSNSilver",
                table: "LuminaUserInfo",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PSNTrophyLevel",
                table: "LuminaUserInfo",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PSNBronze",
                table: "LuminaUserInfo");

            migrationBuilder.DropColumn(
                name: "PSNGold",
                table: "LuminaUserInfo");

            migrationBuilder.DropColumn(
                name: "PSNPlatinum",
                table: "LuminaUserInfo");

            migrationBuilder.DropColumn(
                name: "PSNSilver",
                table: "LuminaUserInfo");

            migrationBuilder.DropColumn(
                name: "PSNTrophyLevel",
                table: "LuminaUserInfo");

            migrationBuilder.RenameColumn(
                name: "PSNAccountId",
                table: "LuminaUserInfo",
                newName: "AccountId");
        }
    }
}
