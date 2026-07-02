using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bluewater.Infra.Migrations
{
    /// <inheritdoc />
    public partial class AddOutingPlanner : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Outings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserGroupInstanceId = table.Column<Guid>(type: "uuid", nullable: false),
                    OutingDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    OutingDateEnd = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    BoatTypeId = table.Column<Guid>(type: "uuid", nullable: true),
                    BoatTypeDifferent = table.Column<string>(type: "text", nullable: true),
                    BoatId = table.Column<Guid>(type: "uuid", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Confirmed = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Outings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Outings_EquipmentTypes_BoatTypeId",
                        column: x => x.BoatTypeId,
                        principalTable: "EquipmentTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Outings_Equipment_BoatId",
                        column: x => x.BoatId,
                        principalTable: "Equipment",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Outings_UserGroupInstances_UserGroupInstanceId",
                        column: x => x.UserGroupInstanceId,
                        principalTable: "UserGroupInstances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OutingChangelogEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OutingId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    Fields = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutingChangelogEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OutingChangelogEntries_Outings_OutingId",
                        column: x => x.OutingId,
                        principalTable: "Outings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OutingParticipants",
                columns: table => new
                {
                    OutingId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<string>(type: "text", nullable: false),
                    Invited = table.Column<bool>(type: "boolean", nullable: false),
                    CheckedIn = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutingParticipants", x => new { x.OutingId, x.UserId });
                    table.ForeignKey(
                        name: "FK_OutingParticipants_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OutingParticipants_Outings_OutingId",
                        column: x => x.OutingId,
                        principalTable: "Outings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OutingChangelogEntries_OutingId",
                table: "OutingChangelogEntries",
                column: "OutingId");

            migrationBuilder.CreateIndex(
                name: "IX_OutingParticipants_UserId",
                table: "OutingParticipants",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Outings_BoatId",
                table: "Outings",
                column: "BoatId");

            migrationBuilder.CreateIndex(
                name: "IX_Outings_BoatTypeId",
                table: "Outings",
                column: "BoatTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Outings_OutingDate",
                table: "Outings",
                column: "OutingDate");

            migrationBuilder.CreateIndex(
                name: "IX_Outings_UserGroupInstanceId",
                table: "Outings",
                column: "UserGroupInstanceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OutingChangelogEntries");

            migrationBuilder.DropTable(
                name: "OutingParticipants");

            migrationBuilder.DropTable(
                name: "Outings");
        }
    }
}
