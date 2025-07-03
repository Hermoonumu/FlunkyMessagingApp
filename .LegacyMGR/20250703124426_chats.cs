using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MessagingApp.Migrations
{
    /// <inheritdoc />
    public partial class chats : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "chatID",
                table: "Messages",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Chat",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Chat", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "UserChatJoinTable",
                columns: table => new
                {
                    ChatID = table.Column<long>(type: "bigint", nullable: false),
                    UserID = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserChatJoinTable", x => new { x.ChatID, x.UserID });
                    table.ForeignKey(
                        name: "FK_UserChatJoinTable_Chats_ChatID",
                        column: x => x.ChatID,
                        principalTable: "Chat",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserChatJoinTable_Users_UserID",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Messages_chatID",
                table: "Messages",
                column: "chatID");

            migrationBuilder.CreateIndex(
                name: "IX_UserChatJoinTable_UserID",
                table: "UserChatJoinTable",
                column: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_Messages_Chat_chatID",
                table: "Messages",
                column: "chatID",
                principalTable: "Chat",
                principalColumn: "ID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Messages_Chat_chatID",
                table: "Messages");

            migrationBuilder.DropTable(
                name: "UserChatJoinTable");

            migrationBuilder.DropTable(
                name: "Chat");

            migrationBuilder.DropIndex(
                name: "IX_Messages_chatID",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "chatID",
                table: "Messages");
        }
    }
}
