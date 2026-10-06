using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 

namespace HMS.Infrastructure.Persistence.Migrations
{
    
    public partial class SeedRoles : Migration
    {
        
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { new Guid("26f3816c-349c-4553-9a4c-ed31bb49105b"), "C6756F61-ABC2-4EC3-AF7A-F5EE3759445E", "Admin", "ADMIN" },
                    { new Guid("c11f02ae-f2b9-4eef-bddf-c00da05630b7"), "0F264C57-F00D-41C1-8362-81C13FCC431E", "Guest", "GUEST" },
                    { new Guid("d8cfa7e9-c1fc-448d-9702-2adc4685c722"), "705A32B5-6239-41F7-8554-B63D01F73914", "Manager", "MANAGER" }
                });
        }

        
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("26f3816c-349c-4553-9a4c-ed31bb49105b"));

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("c11f02ae-f2b9-4eef-bddf-c00da05630b7"));

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("d8cfa7e9-c1fc-448d-9702-2adc4685c722"));
        }
    }
}
