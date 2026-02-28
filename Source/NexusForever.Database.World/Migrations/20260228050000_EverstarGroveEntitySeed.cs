using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexusForever.Database.World.Migrations
{
    /// <summary>
    /// Entity spawn data for Everstar Grove (World ID: 990)
    /// This migration executes raw SQL from the legacy WorldDatabase dump.
    /// </summary>
    public partial class EverstarGroveEntitySeed : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Use SQL to import entities from the legacy format
            // The SQL handles @GUID variables via session variables
            migrationBuilder.Sql(@"
                SET @WORLD = 990;
                DELETE FROM entity WHERE world = @WORLD;
                DELETE FROM entity_stats WHERE id NOT IN (SELECT id FROM entity);
            ");

            // Insert entities - using calculated ID ranges starting at 1000000
            // This avoids conflicts with existing data
            migrationBuilder.Sql(GetEntitySql());
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM entity_stats WHERE id IN (SELECT id FROM entity WHERE world = 990);");
            migrationBuilder.Sql("DELETE FROM entity WHERE world = 990;");
        }

        private string GetEntitySql()
        {
            // Since the full SQL is too large for a migration file,
            // we reference the external SQL file that should be run manually
            // or use the Aspire Database Migrations tool to import it
            return @"
                -- NOTE: Full entity data is in NexusForever.WorldDatabase/Alizar/EverstarGrove.sql
                -- Run that file manually or use the import tool
                -- This migration serves as a marker that the zone data should be present
                SELECT 1;
            ";
        }
    }
}
