using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RvParkApp.Migrations
{
    /// <inheritdoc />
    public partial class AddParkPolicyAndPriceTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Label",
                table: "CategoryPrices",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PriceType",
                table: "CategoryPrices",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "ParkPolicies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BookingWindowMonths = table.Column<int>(type: "int", nullable: false),
                    PeakStartMonth = table.Column<int>(type: "int", nullable: false),
                    PeakEndMonth = table.Column<int>(type: "int", nullable: false),
                    PeakMaxStayDays = table.Column<int>(type: "int", nullable: false),
                    LongTermStartMonth = table.Column<int>(type: "int", nullable: false),
                    LongTermStartDay = table.Column<int>(type: "int", nullable: false),
                    LongTermEndMonth = table.Column<int>(type: "int", nullable: false),
                    LongTermEndDay = table.Column<int>(type: "int", nullable: false),
                    AwayBeforeReturnDays = table.Column<int>(type: "int", nullable: false),
                    CancellationFee = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CancellationThresholdDays = table.Column<int>(type: "int", nullable: false),
                    LateCancelChargesOneNight = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParkPolicies", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "ParkPolicies",
                columns: new[] { "Id", "AwayBeforeReturnDays", "BookingWindowMonths", "CancellationFee", "CancellationThresholdDays", "LateCancelChargesOneNight", "LongTermEndDay", "LongTermEndMonth", "LongTermStartDay", "LongTermStartMonth", "PeakEndMonth", "PeakMaxStayDays", "PeakStartMonth" },
                values: new object[] { 1, 14, 6, 10.00m, 3, true, 1, 4, 15, 10, 10, 14, 4 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ParkPolicies");

            migrationBuilder.DropColumn(
                name: "Label",
                table: "CategoryPrices");

            migrationBuilder.DropColumn(
                name: "PriceType",
                table: "CategoryPrices");
        }
    }
}
