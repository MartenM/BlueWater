using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bluewater.Infra.Migrations
{
    /// <inheritdoc />
    public partial class AddGroupAdministrative : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Administrative",
                table: "UserGroups",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Administrative",
                table: "UserGroups");
        }
    }
}
