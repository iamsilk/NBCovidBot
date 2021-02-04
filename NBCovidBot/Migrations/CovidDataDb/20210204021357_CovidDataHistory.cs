using Microsoft.EntityFrameworkCore.Migrations;

namespace NBCovidBot.Migrations.CovidDataDb
{
    public partial class CovidDataHistory : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ZoneData",
                columns: table => new
                {
                    ZoneNumber = table.Column<int>(nullable: false),
                    LastUpdate = table.Column<long>(nullable: false),
                    ZoneTitle = table.Column<string>(nullable: true),
                    ActiveCases = table.Column<int>(nullable: false),
                    NewToday = table.Column<int>(nullable: false),
                    TotalCases = table.Column<int>(nullable: false),
                    Recovered = table.Column<int>(nullable: false),
                    Deaths = table.Column<int>(nullable: false),
                    Discriminator = table.Column<string>(nullable: false),
                    TravelRelated = table.Column<int>(nullable: true),
                    CloseContact = table.Column<int>(nullable: true),
                    CommTransmission = table.Column<int>(nullable: true),
                    Hospitalized = table.Column<int>(nullable: true),
                    ICU = table.Column<int>(nullable: true),
                    TotalTests = table.Column<int>(nullable: true),
                    UnderInvestigation = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ZoneData", x => new { x.ZoneNumber, x.LastUpdate });
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ZoneData");
        }
    }
}
