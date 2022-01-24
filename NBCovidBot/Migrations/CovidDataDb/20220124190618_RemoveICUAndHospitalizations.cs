using Microsoft.EntityFrameworkCore.Migrations;

namespace NBCovidBot.Migrations.CovidDataDb
{
    public partial class RemoveICUAndHospitalizations : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Hospitalized",
                table: "ZoneData");

            migrationBuilder.DropColumn(
                name: "ICU",
                table: "ZoneData");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Hospitalized",
                table: "ZoneData",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ICU",
                table: "ZoneData",
                type: "int",
                nullable: true);
        }
    }
}
