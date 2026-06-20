using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bluewater.Infra.Migrations
{
    /// <inheritdoc />
    public partial class AddUserProfilePicture : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ProfilePictureFileId",
                table: "AspNetUsers",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_ProfilePictureFileId",
                table: "AspNetUsers",
                column: "ProfilePictureFileId");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_StoredFiles_ProfilePictureFileId",
                table: "AspNetUsers",
                column: "ProfilePictureFileId",
                principalTable: "StoredFiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_StoredFiles_ProfilePictureFileId",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_ProfilePictureFileId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "ProfilePictureFileId",
                table: "AspNetUsers");
        }
    }
}
