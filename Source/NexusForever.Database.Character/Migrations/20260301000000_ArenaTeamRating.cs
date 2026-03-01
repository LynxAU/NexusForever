using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexusForever.Database.Character.Migrations
{
    /// <inheritdoc />
    public partial class ArenaTeamRating : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                @"CREATE TABLE IF NOT EXISTS `guild_arena_team` (
                    `id` bigint(20) unsigned NOT NULL DEFAULT 0,
                    `rating` int(11) NOT NULL DEFAULT 1500,
                    `seasonWins` int(11) NOT NULL DEFAULT 0,
                    `seasonLosses` int(11) NOT NULL DEFAULT 0,
                    PRIMARY KEY (`id`),
                    CONSTRAINT `FK__guild_arena_team_id__guild_id` FOREIGN KEY (`id`) REFERENCES `guild` (`id`)
                  ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TABLE IF EXISTS `guild_arena_team`;");
        }
    }
}
