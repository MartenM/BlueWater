using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bluewater.Infra.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "UserGroups",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                table: "UserGroups",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "UserGroups",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "UserGroups",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "UserGroups",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedByUserId",
                table: "UserGroups",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "UserGroupInstances",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                table: "UserGroupInstances",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "UserGroupInstances",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "UserGroupInstances",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "UserGroupInstances",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedByUserId",
                table: "UserGroupInstances",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "UserGroupCategories",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                table: "UserGroupCategories",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "UserGroupCategories",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "UserGroupCategories",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "UserGroupCategories",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedByUserId",
                table: "UserGroupCategories",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "UserGroups");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "UserGroups");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "UserGroups");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "UserGroups");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "UserGroups");

            migrationBuilder.DropColumn(
                name: "UpdatedByUserId",
                table: "UserGroups");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "UserGroupInstances");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "UserGroupInstances");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "UserGroupInstances");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "UserGroupInstances");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "UserGroupInstances");

            migrationBuilder.DropColumn(
                name: "UpdatedByUserId",
                table: "UserGroupInstances");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "UserGroupCategories");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "UserGroupCategories");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "UserGroupCategories");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "UserGroupCategories");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "UserGroupCategories");

            migrationBuilder.DropColumn(
                name: "UpdatedByUserId",
                table: "UserGroupCategories");
        }
    }
}
