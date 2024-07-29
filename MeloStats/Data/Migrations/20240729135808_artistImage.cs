using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MeloStats.Data.Migrations
{
    /// <inheritdoc />
    public partial class artistImage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SpotifyTokens_UserId",
                table: "SpotifyTokens");

            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "Artists",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SpotifyTokens_UserId",
                table: "SpotifyTokens",
                column: "UserId",
                unique: true,
                filter: "[UserId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SpotifyTokens_UserId",
                table: "SpotifyTokens");

            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "Artists");

            migrationBuilder.AddColumn<int>(
                name: "ExpiresIn",
                table: "SpotifyTokens",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SpotifyTokens_UserId",
                table: "SpotifyTokens",
                column: "UserId");
        }
    }
}
