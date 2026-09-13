using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TimeTracker.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MakeHourTypesGlobal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_HourTypes_Name",
                table: "HourTypes");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "HourTypes");

            migrationBuilder.AddColumn<bool>(
                name: "IsDefault",
                table: "HourTypes",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Dictionary<string, string>>(
                name: "LocalizedNames",
                table: "HourTypes",
                type: "jsonb",
                nullable: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsDefault",
                table: "HourTypes");

            migrationBuilder.DropColumn(
                name: "LocalizedNames",
                table: "HourTypes");

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "HourTypes",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_HourTypes_Name",
                table: "HourTypes",
                column: "Name",
                unique: true);
        }
    }
}
