using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bluewater.Infra.Migrations
{
    /// <inheritdoc />
    public partial class AddMemberClusters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MemberClusters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MemberClusters", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MemberClusterCriteria",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MemberClusterId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    UserGroupCategoryId = table.Column<Guid>(type: "uuid", nullable: true),
                    UserGroupCategoryRoleId = table.Column<Guid>(type: "uuid", nullable: true),
                    UserGroupId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MemberClusterCriteria", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MemberClusterCriteria_MemberClusters_MemberClusterId",
                        column: x => x.MemberClusterId,
                        principalTable: "MemberClusters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MemberClusterCriteria_UserGroupCategories_UserGroupCategory~",
                        column: x => x.UserGroupCategoryId,
                        principalTable: "UserGroupCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MemberClusterCriteria_UserGroupCategoryRoles_UserGroupCateg~",
                        column: x => x.UserGroupCategoryRoleId,
                        principalTable: "UserGroupCategoryRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_MemberClusterCriteria_UserGroups_UserGroupId",
                        column: x => x.UserGroupId,
                        principalTable: "UserGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MemberClusterCriteria_MemberClusterId",
                table: "MemberClusterCriteria",
                column: "MemberClusterId");

            migrationBuilder.CreateIndex(
                name: "IX_MemberClusterCriteria_UserGroupCategoryId",
                table: "MemberClusterCriteria",
                column: "UserGroupCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_MemberClusterCriteria_UserGroupCategoryRoleId",
                table: "MemberClusterCriteria",
                column: "UserGroupCategoryRoleId");

            migrationBuilder.CreateIndex(
                name: "IX_MemberClusterCriteria_UserGroupId",
                table: "MemberClusterCriteria",
                column: "UserGroupId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MemberClusterCriteria");

            migrationBuilder.DropTable(
                name: "MemberClusters");
        }
    }
}
