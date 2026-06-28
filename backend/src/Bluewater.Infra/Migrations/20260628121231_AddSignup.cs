using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bluewater.Infra.Migrations
{
    /// <inheritdoc />
    public partial class AddSignup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SignupCategories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Hidden = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SignupCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Signups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: true),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AllowMultiple = table.Column<bool>(type: "boolean", nullable: false),
                    AllowDelete = table.Column<bool>(type: "boolean", nullable: false),
                    AllowUpdate = table.Column<bool>(type: "boolean", nullable: false),
                    MaxSignups = table.Column<int>(type: "integer", nullable: true),
                    MaxWaitlist = table.Column<int>(type: "integer", nullable: true),
                    HideSignups = table.Column<bool>(type: "boolean", nullable: false),
                    Anonymous = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Signups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Signups_SignupCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "SignupCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "SignupClusters",
                columns: table => new
                {
                    SignupId = table.Column<Guid>(type: "uuid", nullable: false),
                    MemberClusterId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SignupClusters", x => new { x.SignupId, x.MemberClusterId });
                    table.ForeignKey(
                        name: "FK_SignupClusters_MemberClusters_MemberClusterId",
                        column: x => x.MemberClusterId,
                        principalTable: "MemberClusters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SignupClusters_Signups_SignupId",
                        column: x => x.SignupId,
                        principalTable: "Signups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SignupInputFields",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SignupId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Note = table.Column<string>(type: "text", nullable: true),
                    Type = table.Column<string>(type: "text", nullable: false),
                    Options = table.Column<string>(type: "text", nullable: true),
                    Visible = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SignupInputFields", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SignupInputFields_Signups_SignupId",
                        column: x => x.SignupId,
                        principalTable: "Signups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SignupResponses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SignupId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Reservation = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SignupResponses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SignupResponses_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SignupResponses_Signups_SignupId",
                        column: x => x.SignupId,
                        principalTable: "Signups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SignupResponseFieldValues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ResponseId = table.Column<Guid>(type: "uuid", nullable: false),
                    FieldId = table.Column<Guid>(type: "uuid", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SignupResponseFieldValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SignupResponseFieldValues_SignupInputFields_FieldId",
                        column: x => x.FieldId,
                        principalTable: "SignupInputFields",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SignupResponseFieldValues_SignupResponses_ResponseId",
                        column: x => x.ResponseId,
                        principalTable: "SignupResponses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SignupClusters_MemberClusterId",
                table: "SignupClusters",
                column: "MemberClusterId");

            migrationBuilder.CreateIndex(
                name: "IX_SignupInputFields_SignupId",
                table: "SignupInputFields",
                column: "SignupId");

            migrationBuilder.CreateIndex(
                name: "IX_SignupResponseFieldValues_FieldId",
                table: "SignupResponseFieldValues",
                column: "FieldId");

            migrationBuilder.CreateIndex(
                name: "IX_SignupResponseFieldValues_ResponseId",
                table: "SignupResponseFieldValues",
                column: "ResponseId");

            migrationBuilder.CreateIndex(
                name: "IX_SignupResponses_SignupId",
                table: "SignupResponses",
                column: "SignupId");

            migrationBuilder.CreateIndex(
                name: "IX_SignupResponses_UserId",
                table: "SignupResponses",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Signups_CategoryId",
                table: "Signups",
                column: "CategoryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SignupClusters");

            migrationBuilder.DropTable(
                name: "SignupResponseFieldValues");

            migrationBuilder.DropTable(
                name: "SignupInputFields");

            migrationBuilder.DropTable(
                name: "SignupResponses");

            migrationBuilder.DropTable(
                name: "Signups");

            migrationBuilder.DropTable(
                name: "SignupCategories");
        }
    }
}
