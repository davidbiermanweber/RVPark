using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace RvParkApp.Migrations
{
    /// <inheritdoc />
    public partial class AddCategoryPricesAndUpdates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "CategoryPrices",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "CategoryPrices",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "CategoryPrices",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "CategoryPrices",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "CategoryPrices",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "CategoryPrices",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 6);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { 1, "RV Site - Full Hookup (Water/Electric/Sewer)" },
                    { 2, "RV Site - Water & Electric" },
                    { 3, "RV Site - Electric Only" },
                    { 4, "Tent Site (No Hookups)" },
                    { 5, "Dry Storage" },
                    { 6, "Rental Trailer" }
                });

            migrationBuilder.InsertData(
                table: "CategoryPrices",
                columns: new[] { "Id", "CategoryId", "EndDate", "Price", "StartDate" },
                values: new object[,]
                {
                    { 1, 1, null, 50m, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 2, 2, null, 40m, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 3, 3, null, 30m, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 4, 4, null, 20m, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 5, 5, null, 15m, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 6, 6, null, 75m, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) }
                });
        }
    }
}
