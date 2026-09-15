using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ApiTesting.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WeatherForecasts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Date = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    TemperatureC = table.Column<int>(type: "INTEGER", nullable: false),
                    Summary = table.Column<string>(type: "TEXT", nullable: true),
                    Location = table.Column<string>(type: "TEXT", nullable: false),
                    Latitude = table.Column<double>(type: "REAL", nullable: false),
                    Longitude = table.Column<double>(type: "REAL", nullable: false),
                    WindSpeedKmh = table.Column<int>(type: "INTEGER", nullable: false),
                    WindDirectionDegrees = table.Column<int>(type: "INTEGER", nullable: false),
                    HumidityPercent = table.Column<int>(type: "INTEGER", nullable: false),
                    PrecipitationChancePercent = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeatherForecasts", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "WeatherForecasts",
                columns: new[] { "Id", "Date", "HumidityPercent", "Latitude", "Location", "Longitude", "PrecipitationChancePercent", "Summary", "TemperatureC", "WindDirectionDegrees", "WindSpeedKmh" },
                values: new object[,]
                {
                    { 1, new DateOnly(2026, 9, 16), 60, 59.913899999999998, "Oslo", 10.7522, 20, "Mild", 20, 270, 12 },
                    { 2, new DateOnly(2026, 9, 17), 55, 48.8566, "Paris", 2.3521999999999998, 10, "Warm", 23, 180, 8 },
                    { 3, new DateOnly(2026, 9, 18), 70, 52.520000000000003, "Berlin", 13.404999999999999, 40, "Cool", 15, 45, 18 },
                    { 4, new DateOnly(2026, 9, 19), 80, 40.416800000000002, "Madrid", -3.7038000000000002, 60, "Chilly", 5, 315, 25 },
                    { 5, new DateOnly(2026, 9, 20), 35, 41.902799999999999, "Rome", 12.4964, 5, "Hot", 30, 90, 5 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WeatherForecasts");
        }
    }
}
