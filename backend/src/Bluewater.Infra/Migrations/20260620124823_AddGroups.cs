using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bluewater.Infra.Migrations
{
    /// <inheritdoc />
    public partial class AddGroups : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserGroupCategories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserGroupCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserGroups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    UserGroupCategoryId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserGroups_UserGroupCategories_UserGroupCategoryId",
                        column: x => x.UserGroupCategoryId,
                        principalTable: "UserGroupCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserGroupInstances",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserGroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    SeasonId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserGroupInstances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserGroupInstances_Seasons_SeasonId",
                        column: x => x.SeasonId,
                        principalTable: "Seasons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserGroupInstances_UserGroups_UserGroupId",
                        column: x => x.UserGroupId,
                        principalTable: "UserGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserGroupInstanceMembers",
                columns: table => new
                {
                    UserGroupInstanceId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserGroupInstanceMembers", x => new { x.UserGroupInstanceId, x.UserId });
                    table.ForeignKey(
                        name: "FK_UserGroupInstanceMembers_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserGroupInstanceMembers_UserGroupInstances_UserGroupInstan~",
                        column: x => x.UserGroupInstanceId,
                        principalTable: "UserGroupInstances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserGroupInstancePermissions",
                columns: table => new
                {
                    UserGroupInstanceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Permission = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserGroupInstancePermissions", x => new { x.UserGroupInstanceId, x.Permission });
                    table.ForeignKey(
                        name: "FK_UserGroupInstancePermissions_UserGroupInstances_UserGroupIn~",
                        column: x => x.UserGroupInstanceId,
                        principalTable: "UserGroupInstances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserGroupInstanceMembers_UserId",
                table: "UserGroupInstanceMembers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserGroupInstances_SeasonId",
                table: "UserGroupInstances",
                column: "SeasonId");

            migrationBuilder.CreateIndex(
                name: "IX_UserGroupInstances_UserGroupId_SeasonId",
                table: "UserGroupInstances",
                columns: new[] { "UserGroupId", "SeasonId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserGroups_UserGroupCategoryId",
                table: "UserGroups",
                column: "UserGroupCategoryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserGroupInstanceMembers");

            migrationBuilder.DropTable(
                name: "UserGroupInstancePermissions");

            migrationBuilder.DropTable(
                name: "UserGroupInstances");

            migrationBuilder.DropTable(
                name: "UserGroups");

            migrationBuilder.DropTable(
                name: "UserGroupCategories");
        }
    }
}
