using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TimeTracker.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanyWorkSchedule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                table: "WorkSchedules",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.CreateIndex(
                name: "IX_WorkSchedules_TenantId_CompanyDefault",
                table: "WorkSchedules",
                column: "TenantId",
                unique: true,
                filter: "\"UserId\" IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Company schedules have no user, which the old column can't hold.
            migrationBuilder.Sql("""DELETE FROM "WorkSchedules" WHERE "UserId" IS NULL;""");

            migrationBuilder.DropIndex(
                name: "IX_WorkSchedules_TenantId_CompanyDefault",
                table: "WorkSchedules");

            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                table: "WorkSchedules",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }
    }
}
