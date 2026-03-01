using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexusForever.Database.Character.Migrations
{
    public partial class FriendInviteTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CharacterFriendInvite",
                columns: table => new
                {
                    id         = table.Column<ulong>(type: "bigint(20) unsigned", nullable: false),
                    senderId   = table.Column<ulong>(type: "bigint(20) unsigned", nullable: false),
                    receiverId = table.Column<ulong>(type: "bigint(20) unsigned", nullable: false),
                    note       = table.Column<string>(type: "varchar(255)", nullable: false, defaultValue: ""),
                    createdAt  = table.Column<long>(type: "bigint(20)", nullable: false, defaultValue: 0L),
                    seen       = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false, defaultValue: (byte)0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.id);
                    table.ForeignKey(
                        name: "FK__CharacterFriendInvite_senderId__character_id",
                        column: x => x.senderId,
                        principalTable: "character",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK__CharacterFriendInvite_receiverId__character_id",
                        column: x => x.receiverId,
                        principalTable: "character",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_CharacterFriendInvite_senderId",
                table: "CharacterFriendInvite",
                column: "senderId");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterFriendInvite_receiverId",
                table: "CharacterFriendInvite",
                column: "receiverId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "CharacterFriendInvite");
        }
    }
}
