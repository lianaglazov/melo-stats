using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MeloStats.Data.Migrations
{
    /// <inheritdoc />
    public partial class newstuffforartists : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Genres",
                table: "Artists",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Popularity",
                table: "Artists",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Genres",
                table: "Artists");

            migrationBuilder.DropColumn(
                name: "Popularity",
                table: "Artists");
        }
    }
}
