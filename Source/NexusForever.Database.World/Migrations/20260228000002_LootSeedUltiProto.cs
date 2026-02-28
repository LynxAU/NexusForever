using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexusForever.Database.World.Migrations
{
    /// <inheritdoc />
    public partial class LootSeedUltiProto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Ultimate Protogames (WorldId 3041) — 2 bosses, context=2 (Dungeon)
            // Source: jabbithole.com Drop 5 data
            // NOTE: Jabbithole explicitly classifies both bosses as "[Dungeon: Ultimate Protogames]"
            //       with Superb ilvl 80 loot (dungeon tier, not raid tier).
            //       Creature2Ids 61417/61463 are the veteran dungeon versions.
            migrationBuilder.Sql(@"-- ============================================================
-- Ultimate Protogames Dungeon Boss Loot
-- WorldId 3041 — labeled ""[Dungeon: Ultimate Protogames]"" on Jabbithole
-- context = 2 (LootContext.Dungeon), sourceConfidence = 1
-- ============================================================

-- --- CreatureId 61417: Hut-Hut (Intern Disposal Facility 74, Veteran) ---
INSERT IGNORE INTO creature_loot_entry (creatureId, lootGroupId, itemId, context, sourceConfidence, minCount, maxCount, dropRate, evidenceRef, enabled) VALUES
(61417, 1, 61379, 2, 1, 1, 1, 4.27, 'https://www.jabbithole.com/npcs/hut-hut-204-veteran', 1),
(61417, 1, 61373, 2, 1, 1, 1, 4.18, 'https://www.jabbithole.com/npcs/hut-hut-204-veteran', 1),
(61417, 1, 61375, 2, 1, 1, 1, 3.94, 'https://www.jabbithole.com/npcs/hut-hut-204-veteran', 1),
(61417, 1, 61376, 2, 1, 1, 1, 3.90, 'https://www.jabbithole.com/npcs/hut-hut-204-veteran', 1),
(61417, 1, 61370, 2, 1, 1, 1, 3.85, 'https://www.jabbithole.com/npcs/hut-hut-204-veteran', 1),
(61417, 1, 61371, 2, 1, 1, 1, 3.81, 'https://www.jabbithole.com/npcs/hut-hut-204-veteran', 1),
(61417, 1, 61369, 2, 1, 1, 1, 3.53, 'https://www.jabbithole.com/npcs/hut-hut-204-veteran', 1),
(61417, 1, 61367, 2, 1, 1, 1, 3.39, 'https://www.jabbithole.com/npcs/hut-hut-204-veteran', 1),
(61417, 1, 61378, 2, 1, 1, 1, 3.39, 'https://www.jabbithole.com/npcs/hut-hut-204-veteran', 1),
(61417, 1, 61365, 2, 1, 1, 1, 3.34, 'https://www.jabbithole.com/npcs/hut-hut-204-veteran', 1),
(61417, 1, 61377, 2, 1, 1, 1, 3.16, 'https://www.jabbithole.com/npcs/hut-hut-204-veteran', 1),
(61417, 1, 61374, 2, 1, 1, 1, 3.16, 'https://www.jabbithole.com/npcs/hut-hut-204-veteran', 1),
(61417, 1, 61366, 2, 1, 1, 1, 2.83, 'https://www.jabbithole.com/npcs/hut-hut-204-veteran', 1),
(61417, 1, 61368, 2, 1, 1, 1, 2.83, 'https://www.jabbithole.com/npcs/hut-hut-204-veteran', 1),
(61417, 1, 61372, 2, 1, 1, 1, 2.65, 'https://www.jabbithole.com/npcs/hut-hut-204-veteran', 1),
(61417, 1, 49668, 2, 1, 1, 1, 0.51, 'https://www.jabbithole.com/npcs/hut-hut-204-veteran', 1),
(61417, 1, 49667, 2, 1, 1, 1, 0.42, 'https://www.jabbithole.com/npcs/hut-hut-204-veteran', 1);

-- --- CreatureId 61463: Bev-O-Rage (The Break Room, Veteran) ---
INSERT IGNORE INTO creature_loot_entry (creatureId, lootGroupId, itemId, context, sourceConfidence, minCount, maxCount, dropRate, evidenceRef, enabled) VALUES
(61463, 1, 61357, 2, 1, 1, 1, 6.45, 'https://www.jabbithole.com/npcs/bev-o-rage-177-veteran', 1),
(61463, 1, 61361, 2, 1, 1, 1, 6.09, 'https://www.jabbithole.com/npcs/bev-o-rage-177-veteran', 1),
(61463, 1, 61360, 2, 1, 1, 1, 5.80, 'https://www.jabbithole.com/npcs/bev-o-rage-177-veteran', 1),
(61463, 1, 61358, 2, 1, 1, 1, 5.37, 'https://www.jabbithole.com/npcs/bev-o-rage-177-veteran', 1),
(61463, 1, 61364, 2, 1, 1, 1, 5.30, 'https://www.jabbithole.com/npcs/bev-o-rage-177-veteran', 1),
(61463, 1, 61359, 2, 1, 1, 1, 5.16, 'https://www.jabbithole.com/npcs/bev-o-rage-177-veteran', 1),
(61463, 1, 61363, 2, 1, 1, 1, 4.51, 'https://www.jabbithole.com/npcs/bev-o-rage-177-veteran', 1),
(61463, 1, 61362, 2, 1, 1, 1, 4.15, 'https://www.jabbithole.com/npcs/bev-o-rage-177-veteran', 1),
(61463, 1, 61337, 2, 1, 1, 1, 0.07, 'https://www.jabbithole.com/npcs/bev-o-rage-177-veteran', 1),
(61463, 1, 61344, 2, 1, 1, 1, 0.07, 'https://www.jabbithole.com/npcs/bev-o-rage-177-veteran', 1),
(61463, 1, 61339, 2, 1, 1, 1, 0.07, 'https://www.jabbithole.com/npcs/bev-o-rage-177-veteran', 1),
(61463, 1, 61346, 2, 1, 1, 1, 0.14, 'https://www.jabbithole.com/npcs/bev-o-rage-177-veteran', 1);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DELETE FROM creature_loot_entry WHERE creatureId IN (61417, 61463) AND sourceConfidence = 1;");
        }
    }
}
