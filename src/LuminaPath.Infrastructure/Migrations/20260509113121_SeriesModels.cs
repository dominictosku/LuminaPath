using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace LuminaPath.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeriesModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Discriminator",
                table: "Media",
                type: "character varying(8)",
                maxLength: 8,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(5)",
                oldMaxLength: 5);

            migrationBuilder.AddColumn<int>(
                name: "Series_EpisodeCount",
                table: "Media",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Series_ExpectedWatchTimePerEpisodeMinutes",
                table: "Media",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "MySeries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    SeriesId = table.Column<int>(type: "integer", nullable: false),
                    CurrentWatchTimeMinutes = table.Column<int>(type: "integer", nullable: true),
                    CurrentEpisode = table.Column<int>(type: "integer", nullable: true),
                    Rating = table.Column<short>(type: "smallint", nullable: true),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TimeSpend = table.Column<double>(type: "double precision", nullable: true),
                    LuminaUserId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MySeries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MySeries_AspNetUsers_LuminaUserId",
                        column: x => x.LuminaUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MySeries_Media_SeriesId",
                        column: x => x.SeriesId,
                        principalTable: "Media",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MySeries_LuminaUserId",
                table: "MySeries",
                column: "LuminaUserId");

            migrationBuilder.CreateIndex(
                name: "IX_MySeries_SeriesId",
                table: "MySeries",
                column: "SeriesId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MySeries");

            migrationBuilder.DropColumn(
                name: "Series_EpisodeCount",
                table: "Media");

            migrationBuilder.DropColumn(
                name: "Series_ExpectedWatchTimePerEpisodeMinutes",
                table: "Media");

            migrationBuilder.AlterColumn<string>(
                name: "Discriminator",
                table: "Media",
                type: "character varying(5)",
                maxLength: 5,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(8)",
                oldMaxLength: 8);
        }
    }
}
