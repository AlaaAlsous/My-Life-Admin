using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyLife_Admin.Migrations
{
    /// <inheritdoc />
    public partial class AddUserSettingsPreferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "DarkMode",
                table: "UserSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "EmailNotifications",
                table: "UserSettings",
                type: "bit",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DarkMode",
                table: "UserSettings");

            migrationBuilder.DropColumn(
                name: "EmailNotifications",
                table: "UserSettings");
        }
    }
}
