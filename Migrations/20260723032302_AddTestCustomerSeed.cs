using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RvParkApp.Migrations
{
    /// <inheritdoc />
    public partial class AddTestCustomerSeed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "Affiliation", "Email", "EmailVerificationToken", "IsEmailVerified", "MilitaryStatus", "Name", "PasswordHash", "PasswordResetToken", "Phone", "ResetExpiresUtc", "TokenExpiresUtc" },
                values: new object[] { 5738, 7, "test@example.com", null, true, "Test", "Test Customer", "password", null, "555-0000", null, null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 5738);
        }
    }
}
