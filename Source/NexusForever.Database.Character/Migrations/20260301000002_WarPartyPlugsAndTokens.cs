using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexusForever.Database.Character.Migrations
{
    /// <inheritdoc />
    public partial class WarPartyPlugsAndTokens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                @"ALTER TABLE `guild_war_party` 
                  ADD COLUMN `bossTokens` int(11) NOT NULL DEFAULT 0 AFTER `seasonLosses`,
                  ADD COLUMN `plugSlots` varchar(1024) NOT NULL DEFAULT '' AFTER `bossTokens`;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                @"ALTER TABLE `guild_war_party` 
                  DROP COLUMN `bossTokens`,
                  DROP COLUMN `plugSlots`;");
        }
    }
}
