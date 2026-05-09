using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace LuminaPath.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAnimeMovieMedia : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EpisodeCount",
                table: "Media",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ExpectedWatchTimeMinutes",
                table: "Media",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Movie_ExpectedWatchTimeMinutes",
                table: "Media",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "MyAnimes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    AnimeId = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_MyAnimes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MyAnimes_AspNetUsers_LuminaUserId",
                        column: x => x.LuminaUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MyAnimes_Media_AnimeId",
                        column: x => x.AnimeId,
                        principalTable: "Media",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MyMovies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    MovieId = table.Column<int>(type: "integer", nullable: false),
                    CurrentWatchTimeMinutes = table.Column<int>(type: "integer", nullable: true),
                    Rating = table.Column<short>(type: "smallint", nullable: true),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TimeSpend = table.Column<double>(type: "double precision", nullable: true),
                    LuminaUserId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MyMovies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MyMovies_AspNetUsers_LuminaUserId",
                        column: x => x.LuminaUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MyMovies_Media_MovieId",
                        column: x => x.MovieId,
                        principalTable: "Media",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MyAnimes_AnimeId",
                table: "MyAnimes",
                column: "AnimeId");

            migrationBuilder.CreateIndex(
                name: "IX_MyAnimes_LuminaUserId",
                table: "MyAnimes",
                column: "LuminaUserId");

            migrationBuilder.CreateIndex(
                name: "IX_MyMovies_LuminaUserId",
                table: "MyMovies",
                column: "LuminaUserId");

            migrationBuilder.CreateIndex(
                name: "IX_MyMovies_MovieId",
                table: "MyMovies",
                column: "MovieId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MyAnimes");

            migrationBuilder.DropTable(
                name: "MyMovies");

            migrationBuilder.DropColumn(
                name: "EpisodeCount",
                table: "Media");

            migrationBuilder.DropColumn(
                name: "ExpectedWatchTimeMinutes",
                table: "Media");

            migrationBuilder.DropColumn(
                name: "Movie_ExpectedWatchTimeMinutes",
                table: "Media");
        }
    }
}
