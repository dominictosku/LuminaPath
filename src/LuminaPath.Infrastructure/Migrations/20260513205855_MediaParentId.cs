using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LuminaPath.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MediaParentId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ParentAnimeId",
                table: "Media",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ParentGameId",
                table: "Media",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ParentSeriesId",
                table: "Media",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Media_ParentAnimeId",
                table: "Media",
                column: "ParentAnimeId");

            migrationBuilder.CreateIndex(
                name: "IX_Media_ParentGameId",
                table: "Media",
                column: "ParentGameId");

            migrationBuilder.CreateIndex(
                name: "IX_Media_ParentSeriesId",
                table: "Media",
                column: "ParentSeriesId");

            migrationBuilder.AddForeignKey(
                name: "FK_Media_Media_ParentAnimeId",
                table: "Media",
                column: "ParentAnimeId",
                principalTable: "Media",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Media_Media_ParentGameId",
                table: "Media",
                column: "ParentGameId",
                principalTable: "Media",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Media_Media_ParentSeriesId",
                table: "Media",
                column: "ParentSeriesId",
                principalTable: "Media",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Media_Media_ParentAnimeId",
                table: "Media");

            migrationBuilder.DropForeignKey(
                name: "FK_Media_Media_ParentGameId",
                table: "Media");

            migrationBuilder.DropForeignKey(
                name: "FK_Media_Media_ParentSeriesId",
                table: "Media");

            migrationBuilder.DropIndex(
                name: "IX_Media_ParentAnimeId",
                table: "Media");

            migrationBuilder.DropIndex(
                name: "IX_Media_ParentGameId",
                table: "Media");

            migrationBuilder.DropIndex(
                name: "IX_Media_ParentSeriesId",
                table: "Media");

            migrationBuilder.DropColumn(
                name: "ParentAnimeId",
                table: "Media");

            migrationBuilder.DropColumn(
                name: "ParentGameId",
                table: "Media");

            migrationBuilder.DropColumn(
                name: "ParentSeriesId",
                table: "Media");
        }
    }
}
