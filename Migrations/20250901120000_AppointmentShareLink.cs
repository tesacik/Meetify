using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Meetify.Migrations
{
    public partial class AppointmentShareLink : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ShareLinkId",
                table: "Appointments",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: Guid.Empty);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ShareLinkId",
                table: "Appointments");
        }
    }
}

