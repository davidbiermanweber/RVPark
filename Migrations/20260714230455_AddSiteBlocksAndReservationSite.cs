using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RvParkApp.Migrations
{
    /// <inheritdoc />
    public partial class AddSiteBlocksAndReservationSite : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Sites",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxRvLength",
                table: "Sites",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SiteId",
                table: "Reservations",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SiteBlocks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SiteId = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedByEmployeeId = table.Column<int>(type: "int", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SiteBlocks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SiteBlocks_Sites_SiteId",
                        column: x => x.SiteId,
                        principalTable: "Sites",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_SiteId",
                table: "Reservations",
                column: "SiteId");

            migrationBuilder.CreateIndex(
                name: "IX_SiteBlocks_SiteId",
                table: "SiteBlocks",
                column: "SiteId");

            migrationBuilder.AddForeignKey(
                name: "FK_Reservations_Sites_SiteId",
                table: "Reservations",
                column: "SiteId",
                principalTable: "Sites",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reservations_Sites_SiteId",
                table: "Reservations");

            migrationBuilder.DropTable(
                name: "SiteBlocks");

            migrationBuilder.DropIndex(
                name: "IX_Reservations_SiteId",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Sites");

            migrationBuilder.DropColumn(
                name: "MaxRvLength",
                table: "Sites");

            migrationBuilder.DropColumn(
                name: "SiteId",
                table: "Reservations");
        }
    }
}
