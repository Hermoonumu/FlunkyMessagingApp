using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MessagingApp.Migrations
{
    /// <inheritdoc />
    public partial class chatsFinalIHope : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Messages_Chat_ChatID",
                table: "Messages");

            migrationBuilder.DropForeignKey(
                name: "FK_Messages_Chat_chatID",
                table: "Messages");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Chat",
                table: "Chat");

            migrationBuilder.RenameTable(
                name: "Chat",
                newName: "Chats");

            migrationBuilder.AddColumn<long>(
                name: "OwnerID",
                table: "Chats",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Chats",
                table: "Chats",
                column: "ID");

            migrationBuilder.CreateIndex(
                name: "IX_Chats_OwnerID",
                table: "Chats",
                column: "OwnerID",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Chats_Users_OwnerID",
                table: "Chats",
                column: "OwnerID",
                principalTable: "Users",
                principalColumn: "ID");

            migrationBuilder.AddForeignKey(
                name: "FK_Messages_Chats_ChatID",
                table: "Messages",
                column: "ChatID",
                principalTable: "Chats",
                principalColumn: "ID");

            migrationBuilder.AddForeignKey(
                name: "FK_Messages_Chats_chatID",
                table: "Messages",
                column: "chatID",
                principalTable: "Chats",
                principalColumn: "ID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Chats_Users_OwnerID",
                table: "Chats");

            migrationBuilder.DropForeignKey(
                name: "FK_Messages_Chats_ChatID",
                table: "Messages");

            migrationBuilder.DropForeignKey(
                name: "FK_Messages_Chats_chatID",
                table: "Messages");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Chats",
                table: "Chats");

            migrationBuilder.DropIndex(
                name: "IX_Chats_OwnerID",
                table: "Chats");

            migrationBuilder.DropColumn(
                name: "OwnerID",
                table: "Chats");

            migrationBuilder.RenameTable(
                name: "Chats",
                newName: "Chat");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Chat",
                table: "Chat",
                column: "ID");

            migrationBuilder.AddForeignKey(
                name: "FK_Messages_Chat_ChatID",
                table: "Messages",
                column: "ChatID",
                principalTable: "Chat",
                principalColumn: "ID");

            migrationBuilder.AddForeignKey(
                name: "FK_Messages_Chat_chatID",
                table: "Messages",
                column: "chatID",
                principalTable: "Chat",
                principalColumn: "ID");
        }
    }
}
