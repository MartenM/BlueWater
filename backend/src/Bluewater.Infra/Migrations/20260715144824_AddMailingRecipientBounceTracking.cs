using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bluewater.Infra.Migrations
{
    /// <inheritdoc />
    public partial class AddMailingRecipientBounceTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BounceReason",
                table: "MailingRecipients",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Bounced",
                table: "MailingRecipients",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "FailedAt",
                table: "MailingRecipients",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BounceReason",
                table: "MailingRecipients");

            migrationBuilder.DropColumn(
                name: "Bounced",
                table: "MailingRecipients");

            migrationBuilder.DropColumn(
                name: "FailedAt",
                table: "MailingRecipients");
        }
    }
}
