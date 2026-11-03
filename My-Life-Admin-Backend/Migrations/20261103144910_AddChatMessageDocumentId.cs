using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyLife_Admin.Migrations
{
    /// <inheritdoc />
    public partial class AddChatMessageDocumentId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DocumentId",
                table: "ChatMessages",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_DocumentId",
                table: "ChatMessages",
                column: "DocumentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ChatMessages_DocumentId",
                table: "ChatMessages");

            migrationBuilder.DropColumn(
                name: "DocumentId",
                table: "ChatMessages");
        }
    }
}
