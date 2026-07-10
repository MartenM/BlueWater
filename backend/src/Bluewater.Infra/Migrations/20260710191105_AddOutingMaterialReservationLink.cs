using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bluewater.Infra.Migrations
{
    /// <inheritdoc />
    public partial class AddOutingMaterialReservationLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "OutingId",
                table: "MaterialReservations",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MaterialReservations_OutingId",
                table: "MaterialReservations",
                column: "OutingId");

            migrationBuilder.AddForeignKey(
                name: "FK_MaterialReservations_Outings_OutingId",
                table: "MaterialReservations",
                column: "OutingId",
                principalTable: "Outings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MaterialReservations_Outings_OutingId",
                table: "MaterialReservations");

            migrationBuilder.DropIndex(
                name: "IX_MaterialReservations_OutingId",
                table: "MaterialReservations");

            migrationBuilder.DropColumn(
                name: "OutingId",
                table: "MaterialReservations");
        }
    }
}
