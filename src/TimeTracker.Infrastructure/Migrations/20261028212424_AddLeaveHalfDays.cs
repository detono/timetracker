using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TimeTracker.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLeaveHalfDays : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FirstDayPart",
                table: "LeaveRequests",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "Full");

            migrationBuilder.AddColumn<string>(
                name: "LastDayPart",
                table: "LeaveRequests",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "Full");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FirstDayPart",
                table: "LeaveRequests");

            migrationBuilder.DropColumn(
                name: "LastDayPart",
                table: "LeaveRequests");
        }
    }
}
