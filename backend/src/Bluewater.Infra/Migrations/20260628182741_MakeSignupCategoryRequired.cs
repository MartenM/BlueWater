using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bluewater.Infra.Migrations
{
    /// <inheritdoc />
    public partial class MakeSignupCategoryRequired : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Signups_SignupCategories_CategoryId",
                table: "Signups");

            migrationBuilder.AlterColumn<Guid>(
                name: "CategoryId",
                table: "Signups",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Signups_SignupCategories_CategoryId",
                table: "Signups",
                column: "CategoryId",
                principalTable: "SignupCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Signups_SignupCategories_CategoryId",
                table: "Signups");

            migrationBuilder.AlterColumn<Guid>(
                name: "CategoryId",
                table: "Signups",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddForeignKey(
                name: "FK_Signups_SignupCategories_CategoryId",
                table: "Signups",
                column: "CategoryId",
                principalTable: "SignupCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
