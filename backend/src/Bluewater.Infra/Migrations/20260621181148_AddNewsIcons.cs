using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bluewater.Infra.Migrations
{
    /// <inheritdoc />
    public partial class AddNewsIcons : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "IconId",
                table: "NewsPosts",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "NewsIcons",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    FileId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NewsIcons", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NewsIcons_StoredFiles_FileId",
                        column: x => x.FileId,
                        principalTable: "StoredFiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NewsPosts_IconId",
                table: "NewsPosts",
                column: "IconId");

            migrationBuilder.CreateIndex(
                name: "IX_NewsIcons_FileId",
                table: "NewsIcons",
                column: "FileId");

            migrationBuilder.AddForeignKey(
                name: "FK_NewsPosts_NewsIcons_IconId",
                table: "NewsPosts",
                column: "IconId",
                principalTable: "NewsIcons",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_NewsPosts_NewsIcons_IconId",
                table: "NewsPosts");

            migrationBuilder.DropTable(
                name: "NewsIcons");

            migrationBuilder.DropIndex(
                name: "IX_NewsPosts_IconId",
                table: "NewsPosts");

            migrationBuilder.DropColumn(
                name: "IconId",
                table: "NewsPosts");
        }
    }
}
