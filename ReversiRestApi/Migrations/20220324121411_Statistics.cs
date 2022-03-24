using Microsoft.EntityFrameworkCore.Migrations;

namespace ReversiRestApi.Migrations
{
    public partial class Statistics : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "GameFinished",
                table: "Spellen",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "GameWinner",
                table: "Spellen",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GameWinnerPlayerToken",
                table: "Spellen",
                type: "nvarchar(max)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GameFinished",
                table: "Spellen");

            migrationBuilder.DropColumn(
                name: "GameWinner",
                table: "Spellen");

            migrationBuilder.DropColumn(
                name: "GameWinnerPlayerToken",
                table: "Spellen");
        }
    }
}
