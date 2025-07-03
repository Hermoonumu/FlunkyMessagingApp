using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MessagingApp.Migrations
{
    /// <inheritdoc />
    public partial class someMoreWiringUp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "ChatID",
                table: "Messages",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "Chat",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_ChatID",
                table: "Messages",
                column: "ChatID");

            migrationBuilder.AddForeignKey(
                name: "FK_Messages_Chat_ChatID",
                table: "Messages",
                column: "ChatID",
                principalTable: "Chat",
                principalColumn: "ID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Messages_Chat_ChatID",
                table: "Messages");

            migrationBuilder.DropIndex(
                name: "IX_Messages_ChatID",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "ChatID",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "Chat");
        }
    }
}
