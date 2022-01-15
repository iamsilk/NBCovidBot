using Microsoft.EntityFrameworkCore.Migrations;

namespace NBCovidBot.Migrations.CovidDataDb
{
    public partial class AddRapidTestsAndAlertLevels : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "NewRapidTestPositives",
                table: "ZoneData",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TotalRapidTestPositives",
                table: "ZoneData",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NewRapidTestPositives",
                table: "ZoneData");

            migrationBuilder.DropColumn(
                name: "TotalRapidTestPositives",
                table: "ZoneData");
        }
    }
}
