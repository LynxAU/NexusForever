using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexusForever.Database.World.Migrations
{
    /// <inheritdoc />
    public partial class _20260228170500_WorldSchemaSync : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                @"CREATE TABLE IF NOT EXISTS `creature_loot_entry` (
                    `creatureId` int(10) unsigned NOT NULL DEFAULT 0,
                    `lootGroupId` int(10) unsigned NOT NULL DEFAULT 0,
                    `itemId` int(10) unsigned NOT NULL DEFAULT 0,
                    `context` tinyint(3) unsigned NOT NULL DEFAULT 0,
                    `sourceConfidence` tinyint(3) unsigned NOT NULL DEFAULT 0,
                    `minCount` int(10) unsigned NOT NULL DEFAULT 1,
                    `maxCount` int(10) unsigned NOT NULL DEFAULT 1,
                    `dropRate` float NOT NULL DEFAULT 1,
                    `evidenceRef` varchar(255) NOT NULL DEFAULT '',
                    `enabled` tinyint(1) NOT NULL DEFAULT 1,
                    PRIMARY KEY (`creatureId`,`lootGroupId`,`itemId`,`context`,`sourceConfidence`),
                    KEY `ix_creature_loot_entry_creature_context_enabled` (`creatureId`,`context`,`enabled`)
                  ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TABLE IF EXISTS `creature_loot_entry`;");
        }
    }
}
