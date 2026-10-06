using HMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HMS.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(HmsDbContext))]
    [Migration("20261006190000_OneManagerPerHotel")]
    public partial class OneManagerPerHotel : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Managers_HotelId",
                table: "Managers");

            migrationBuilder.CreateIndex(
                name: "UQ_Managers_HotelId",
                table: "Managers",
                column: "HotelId",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UQ_Managers_HotelId",
                table: "Managers");

            migrationBuilder.CreateIndex(
                name: "IX_Managers_HotelId",
                table: "Managers",
                column: "HotelId");
        }
    }
}
