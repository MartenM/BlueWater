using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bluewater.Infra.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditFieldsToMembershipAndPermission : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "UserGroupInstancePermissions",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                table: "UserGroupInstancePermissions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "UserGroupInstancePermissions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "UserGroupInstancePermissions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "UserGroupInstancePermissions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedByUserId",
                table: "UserGroupInstancePermissions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "UserGroupInstanceMembers",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                table: "UserGroupInstanceMembers",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "UserGroupInstanceMembers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "UserGroupInstanceMembers",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "UserGroupInstanceMembers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedByUserId",
                table: "UserGroupInstanceMembers",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "UserGroupInstancePermissions");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "UserGroupInstancePermissions");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "UserGroupInstancePermissions");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "UserGroupInstancePermissions");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "UserGroupInstancePermissions");

            migrationBuilder.DropColumn(
                name: "UpdatedByUserId",
                table: "UserGroupInstancePermissions");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "UserGroupInstanceMembers");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "UserGroupInstanceMembers");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "UserGroupInstanceMembers");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "UserGroupInstanceMembers");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "UserGroupInstanceMembers");

            migrationBuilder.DropColumn(
                name: "UpdatedByUserId",
                table: "UserGroupInstanceMembers");
        }
    }
}
