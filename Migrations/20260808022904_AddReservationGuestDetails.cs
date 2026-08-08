using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RvParkApp.Migrations
{
    /// <inheritdoc />
    public partial class AddReservationGuestDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Columns already exist in the database — no action needed.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HasPets",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "NumberOfAdults",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "NumberOfChildren",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "SpecialRequests",
                table: "Reservations");
        }
    }
}
