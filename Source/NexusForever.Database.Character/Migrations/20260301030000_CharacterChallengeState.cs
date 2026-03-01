using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexusForever.Database.Character.Migrations
{
    public partial class CharacterChallengeState : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "character_challenge",
                columns: table => new
                {
                    id = table.Column<ulong>(type: "bigint(20) unsigned", nullable: false),
                    challengeId = table.Column<uint>(type: "int(10) unsigned", nullable: false),
                    currentCount = table.Column<uint>(type: "int(10) unsigned", nullable: false, defaultValue: 0u),
                    currentTier = table.Column<uint>(type: "int(10) unsigned", nullable: false, defaultValue: 0u),
                    completionCount = table.Column<uint>(type: "int(10) unsigned", nullable: false, defaultValue: 0u),
                    isUnlocked = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    isActivated = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    isCompleted = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    isOnCooldown = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    timeRemaining = table.Column<double>(type: "double", nullable: false, defaultValue: 0d),
                    cooldownRemaining = table.Column<double>(type: "double", nullable: false, defaultValue: 0d),
                    activatedDt = table.Column<uint>(type: "int(10) unsigned", nullable: false, defaultValue: 0u)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.id, x.challengeId });
                    table.ForeignKey(
                        name: "FK__character_challenge_id__character_id",
                        column: x => x.id,
                        principalTable: "character",
                        principalColumn: "id");
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "character_challenge");
        }
    }
}
