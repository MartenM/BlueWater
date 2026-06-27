using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bluewater.Infra.Migrations
{
    /// <inheritdoc />
    public partial class EquipmentRequiredExamType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "RequiredExamTypeId",
                table: "Equipment",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Equipment_RequiredExamTypeId",
                table: "Equipment",
                column: "RequiredExamTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Equipment_ExamTypes_RequiredExamTypeId",
                table: "Equipment",
                column: "RequiredExamTypeId",
                principalTable: "ExamTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Equipment_ExamTypes_RequiredExamTypeId",
                table: "Equipment");

            migrationBuilder.DropIndex(
                name: "IX_Equipment_RequiredExamTypeId",
                table: "Equipment");

            migrationBuilder.DropColumn(
                name: "RequiredExamTypeId",
                table: "Equipment");
        }
    }
}
