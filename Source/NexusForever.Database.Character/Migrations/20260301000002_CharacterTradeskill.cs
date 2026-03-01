using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexusForever.Database.Character.Migrations
{
    /// <inheritdoc />
    public partial class CharacterTradeskill : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                @"CREATE TABLE IF NOT EXISTS `character_tradeskill` (
                    `id` bigint(20) unsigned NOT NULL,
                    `tradeskillId` int(10) unsigned NOT NULL,
                    `currentXp` int(10) unsigned NOT NULL DEFAULT 0,
                    `currentTier` tinyint(3) unsigned NOT NULL DEFAULT 0,
                    PRIMARY KEY (`id`, `tradeskillId`),
                    CONSTRAINT `FK__character_tradeskill_id__character_id` FOREIGN KEY (`id`) REFERENCES `character` (`id`)
                  ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TABLE IF EXISTS `character_tradeskill`;");
        }
    }
}
