using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bluewater.Infra.Migrations
{
    /// <inheritdoc />
    public partial class AddMaterialPlanner : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MaterialPlannerEndHour",
                table: "AppSettings",
                type: "integer",
                nullable: false,
                defaultValue: 23);

            migrationBuilder.AddColumn<int>(
                name: "MaterialPlannerStartHour",
                table: "AppSettings",
                type: "integer",
                nullable: false,
                defaultValue: 6);

            migrationBuilder.CreateTable(
                name: "MaterialReservations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EquipmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    EndTime = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    CustomLabel = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaterialReservations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MaterialReservations_Equipment_EquipmentId",
                        column: x => x.EquipmentId,
                        principalTable: "Equipment",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MaterialReservations_EquipmentId_Date",
                table: "MaterialReservations",
                columns: new[] { "EquipmentId", "Date" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MaterialReservations");

            migrationBuilder.DropColumn(
                name: "MaterialPlannerEndHour",
                table: "AppSettings");

            migrationBuilder.DropColumn(
                name: "MaterialPlannerStartHour",
                table: "AppSettings");
        }
    }
}
