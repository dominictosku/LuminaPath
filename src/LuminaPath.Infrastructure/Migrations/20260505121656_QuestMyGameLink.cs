using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LuminaPath.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class QuestMyGameLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MyGameId",
                table: "Quests",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Quests_MyGameId",
                table: "Quests",
                column: "MyGameId");

            migrationBuilder.AddForeignKey(
                name: "FK_Quests_MyGames_MyGameId",
                table: "Quests",
                column: "MyGameId",
                principalTable: "MyGames",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Quests_MyGames_MyGameId",
                table: "Quests");

            migrationBuilder.DropIndex(
                name: "IX_Quests_MyGameId",
                table: "Quests");

            migrationBuilder.DropColumn(
                name: "MyGameId",
                table: "Quests");
        }
    }
}
