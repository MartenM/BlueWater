using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bluewater.Infra.Migrations
{
    /// <inheritdoc />
    public partial class AddMailingTool : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Mailings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Subject = table.Column<string>(type: "text", nullable: false),
                    BodyMarkdown = table.Column<string>(type: "text", nullable: false),
                    SenderKey = table.Column<string>(type: "text", nullable: false),
                    TemplateId = table.Column<Guid>(type: "uuid", nullable: true),
                    LayoutId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    ProofSendCount = table.Column<int>(type: "integer", nullable: false),
                    SentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Mailings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Mailings_MailLayouts_LayoutId",
                        column: x => x.LayoutId,
                        principalTable: "MailLayouts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Mailings_MailTemplates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "MailTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "MailingRecipients",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MailingId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Email = table.Column<string>(type: "text", nullable: false),
                    FullName = table.Column<string>(type: "text", nullable: false),
                    Sent = table.Column<bool>(type: "boolean", nullable: false),
                    SentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Opened = table.Column<bool>(type: "boolean", nullable: false),
                    FirstOpenedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    OpenCount = table.Column<int>(type: "integer", nullable: false),
                    TrackingToken = table.Column<string>(type: "text", nullable: false),
                    RenderedSubject = table.Column<string>(type: "text", nullable: true),
                    RenderedHtmlBody = table.Column<string>(type: "text", nullable: true),
                    RenderedPlainTextBody = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MailingRecipients", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MailingRecipients_Mailings_MailingId",
                        column: x => x.MailingId,
                        principalTable: "Mailings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MailingTargetClusters",
                columns: table => new
                {
                    MailingId = table.Column<Guid>(type: "uuid", nullable: false),
                    MemberClusterId = table.Column<Guid>(type: "uuid", nullable: false),
                    LastSentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MailingTargetClusters", x => new { x.MailingId, x.MemberClusterId });
                    table.ForeignKey(
                        name: "FK_MailingTargetClusters_Mailings_MailingId",
                        column: x => x.MailingId,
                        principalTable: "Mailings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MailingTargetClusters_MemberClusters_MemberClusterId",
                        column: x => x.MemberClusterId,
                        principalTable: "MemberClusters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MailingTargetGroupInstances",
                columns: table => new
                {
                    MailingId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserGroupInstanceId = table.Column<Guid>(type: "uuid", nullable: false),
                    LastSentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MailingTargetGroupInstances", x => new { x.MailingId, x.UserGroupInstanceId });
                    table.ForeignKey(
                        name: "FK_MailingTargetGroupInstances_Mailings_MailingId",
                        column: x => x.MailingId,
                        principalTable: "Mailings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MailingTargetGroupInstances_UserGroupInstances_UserGroupIns~",
                        column: x => x.UserGroupInstanceId,
                        principalTable: "UserGroupInstances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MailingRecipientLinks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MailingRecipientId = table.Column<Guid>(type: "uuid", nullable: false),
                    OriginalUrl = table.Column<string>(type: "text", nullable: false),
                    Token = table.Column<string>(type: "text", nullable: false),
                    ClickCount = table.Column<int>(type: "integer", nullable: false),
                    FirstClickedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MailingRecipientLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MailingRecipientLinks_MailingRecipients_MailingRecipientId",
                        column: x => x.MailingRecipientId,
                        principalTable: "MailingRecipients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MailingRecipientLinks_MailingRecipientId",
                table: "MailingRecipientLinks",
                column: "MailingRecipientId");

            migrationBuilder.CreateIndex(
                name: "IX_MailingRecipientLinks_Token",
                table: "MailingRecipientLinks",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MailingRecipients_MailingId_Email",
                table: "MailingRecipients",
                columns: new[] { "MailingId", "Email" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MailingRecipients_TrackingToken",
                table: "MailingRecipients",
                column: "TrackingToken",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Mailings_LayoutId",
                table: "Mailings",
                column: "LayoutId");

            migrationBuilder.CreateIndex(
                name: "IX_Mailings_TemplateId",
                table: "Mailings",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_MailingTargetClusters_MemberClusterId",
                table: "MailingTargetClusters",
                column: "MemberClusterId");

            migrationBuilder.CreateIndex(
                name: "IX_MailingTargetGroupInstances_UserGroupInstanceId",
                table: "MailingTargetGroupInstances",
                column: "UserGroupInstanceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MailingRecipientLinks");

            migrationBuilder.DropTable(
                name: "MailingTargetClusters");

            migrationBuilder.DropTable(
                name: "MailingTargetGroupInstances");

            migrationBuilder.DropTable(
                name: "MailingRecipients");

            migrationBuilder.DropTable(
                name: "Mailings");
        }
    }
}
