using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexusForever.Database.World.Migrations
{
    /// <inheritdoc />
    public partial class LootSeedRMTandAugLab : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ═══════════════════════════════════════════════════════════════════════════
            // Red Moon Terror (Raid, WorldId 3032/3102) — 5 required bosses
            //
            // Loot source: Item2.tbl client-data inference (no Jabbithole crowdsource data
            // exists for RMT because the raid used a token+vendor model, not direct drops).
            //
            // Token system note: RMT awarded "Marauder Insignia" (74906) per boss kill,
            // spendable at The Bloodied reputation vendor for ilvl 130+ gear. The Grimvoid
            // Marauder Kits (74945/74952/74959) are fortune boxes containing class-specific
            // Marauder armor sets. Weapon cosmetics (74900-74905) and armor cosmetics
            // (73636-73641) were confirmed rare drops from the Skullcano Mordechai page
            // (jabbithole.com/npcs/mordechai-redmoon-20-veteran) and attributed to RMT.
            //
            // Generic raid tokens 42569 (Partial Primal Pattern) and 38769 (Tarnished Eldan
            // Gift) are confirmed shared drops from Mordechai at Skullcano; included here
            // at similar rates for all RMT bosses.
            //
            // Drop rates are best-effort approximations. context=5 (Raid). sourceConfidence=1.
            // ═══════════════════════════════════════════════════════════════════════════

            // ── Redmoon Engineers — Hammer (65758) ─────────────────────────────────────
            migrationBuilder.Sql(@"
INSERT IGNORE INTO creature_loot_entry (creatureId, lootGroupId, itemId, context, sourceConfidence, minCount, maxCount, dropRate, evidenceRef, enabled) VALUES
(65758, 1, 74906, 5, 1, 1, 1, 25.00, 'Item2.tbl:74906 Marauder Insignia', 1),
(65758, 1, 42569, 5, 1, 1, 1, 15.00, 'Item2.tbl:42569 Partial Primal Pattern', 1),
(65758, 1, 38769, 5, 1, 1, 1, 12.00, 'Item2.tbl:38769 Tarnished Eldan Gift', 1),
(65758, 1, 49981, 5, 1, 1, 1,  6.00, 'Item2.tbl:49981 Eldan Runic Module', 1),
(65758, 1, 74945, 5, 1, 1, 1,  5.00, 'Item2.tbl:74945 Grimvoid Marauder Kit', 1),
(65758, 1, 74900, 5, 1, 1, 1,  2.50, 'Item2.tbl:74900 Marauder Psyblade cosmetic', 1),
(65758, 1, 74903, 5, 1, 1, 1,  2.50, 'Item2.tbl:74903 Marauder Launcher cosmetic', 1),
(65758, 1, 73636, 5, 1, 1, 1,  2.00, 'Item2.tbl:73636 Marauder Helm cosmetic', 1),
(65758, 1, 73639, 5, 1, 1, 1,  2.00, 'Item2.tbl:73639 Marauder Shoulders cosmetic', 1);
");

            // ── Redmoon Engineers — Gun (65759) ────────────────────────────────────────
            migrationBuilder.Sql(@"
INSERT IGNORE INTO creature_loot_entry (creatureId, lootGroupId, itemId, context, sourceConfidence, minCount, maxCount, dropRate, evidenceRef, enabled) VALUES
(65759, 1, 74906, 5, 1, 1, 1, 25.00, 'Item2.tbl:74906 Marauder Insignia', 1),
(65759, 1, 42569, 5, 1, 1, 1, 15.00, 'Item2.tbl:42569 Partial Primal Pattern', 1),
(65759, 1, 38769, 5, 1, 1, 1, 12.00, 'Item2.tbl:38769 Tarnished Eldan Gift', 1),
(65759, 1, 49981, 5, 1, 1, 1,  6.00, 'Item2.tbl:49981 Eldan Runic Module', 1),
(65759, 1, 74952, 5, 1, 1, 1,  5.00, 'Item2.tbl:74952 Grimvoid Marauder Kit', 1),
(65759, 1, 74901, 5, 1, 1, 1,  2.50, 'Item2.tbl:74901 Marauder Greatsword cosmetic', 1),
(65759, 1, 74905, 5, 1, 1, 1,  2.50, 'Item2.tbl:74905 Marauder Pistols cosmetic', 1),
(65759, 1, 73637, 5, 1, 1, 1,  2.00, 'Item2.tbl:73637 Marauder Chest cosmetic', 1),
(65759, 1, 73640, 5, 1, 1, 1,  2.00, 'Item2.tbl:73640 Marauder Gloves cosmetic', 1);
");

            // ── Robomination (66085) ───────────────────────────────────────────────────
            migrationBuilder.Sql(@"
INSERT IGNORE INTO creature_loot_entry (creatureId, lootGroupId, itemId, context, sourceConfidence, minCount, maxCount, dropRate, evidenceRef, enabled) VALUES
(66085, 1, 74906, 5, 1, 1, 1, 25.00, 'Item2.tbl:74906 Marauder Insignia', 1),
(66085, 1, 42569, 5, 1, 1, 1, 15.00, 'Item2.tbl:42569 Partial Primal Pattern', 1),
(66085, 1, 38769, 5, 1, 1, 1, 12.00, 'Item2.tbl:38769 Tarnished Eldan Gift', 1),
(66085, 1, 49981, 5, 1, 1, 1,  6.00, 'Item2.tbl:49981 Eldan Runic Module', 1),
(66085, 1, 74959, 5, 1, 1, 1,  5.00, 'Item2.tbl:74959 Grimvoid Marauder Kit', 1),
(66085, 1, 74902, 5, 1, 1, 1,  2.50, 'Item2.tbl:74902 Marauder Resonators cosmetic', 1),
(66085, 1, 74904, 5, 1, 1, 1,  2.50, 'Item2.tbl:74904 Marauder Claws cosmetic', 1),
(66085, 1, 73638, 5, 1, 1, 1,  2.00, 'Item2.tbl:73638 Marauder Pants cosmetic', 1),
(66085, 1, 73641, 5, 1, 1, 1,  2.00, 'Item2.tbl:73641 Marauder Boots cosmetic', 1);
");

            // ── Mordechai Redmoon (65800) — penultimate boss ───────────────────────────
            // Item IDs 61138-61153 confirmed from jabbithole.com/npcs/mordechai-redmoon-20-veteran
            migrationBuilder.Sql(@"
INSERT IGNORE INTO creature_loot_entry (creatureId, lootGroupId, itemId, context, sourceConfidence, minCount, maxCount, dropRate, evidenceRef, enabled) VALUES
(65800, 1, 74906, 5, 1, 1, 1, 25.00, 'Item2.tbl:74906 Marauder Insignia', 1),
(65800, 1, 42569, 5, 1, 1, 1, 18.36, 'https://www.jabbithole.com/npcs/mordechai-redmoon-20-veteran', 1),
(65800, 1, 38769, 5, 1, 1, 1, 11.00, 'https://www.jabbithole.com/npcs/mordechai-redmoon-20-veteran', 1),
(65800, 1, 49981, 5, 1, 1, 1,  6.25, 'https://www.jabbithole.com/npcs/mordechai-redmoon-20-veteran', 1),
(65800, 1, 74945, 5, 1, 1, 1,  8.00, 'Item2.tbl:74945 Grimvoid Marauder Kit (bonus boss)', 1),
(65800, 1, 61143, 5, 1, 1, 1,  2.53, 'https://www.jabbithole.com/npcs/mordechai-redmoon-20-veteran', 1),
(65800, 1, 61148, 5, 1, 1, 1,  2.50, 'https://www.jabbithole.com/npcs/mordechai-redmoon-20-veteran', 1),
(65800, 1, 61150, 5, 1, 1, 1,  2.50, 'https://www.jabbithole.com/npcs/mordechai-redmoon-20-veteran', 1),
(65800, 1, 61138, 5, 1, 1, 1,  2.40, 'https://www.jabbithole.com/npcs/mordechai-redmoon-20-veteran', 1),
(65800, 1, 61140, 5, 1, 1, 1,  2.30, 'https://www.jabbithole.com/npcs/mordechai-redmoon-20-veteran', 1),
(65800, 1, 61142, 5, 1, 1, 1,  2.30, 'https://www.jabbithole.com/npcs/mordechai-redmoon-20-veteran', 1),
(65800, 1, 61144, 5, 1, 1, 1,  2.20, 'https://www.jabbithole.com/npcs/mordechai-redmoon-20-veteran', 1),
(65800, 1, 61146, 5, 1, 1, 1,  2.20, 'https://www.jabbithole.com/npcs/mordechai-redmoon-20-veteran', 1),
(65800, 1, 61149, 5, 1, 1, 1,  2.10, 'https://www.jabbithole.com/npcs/mordechai-redmoon-20-veteran', 1),
(65800, 1, 61151, 5, 1, 1, 1,  2.10, 'https://www.jabbithole.com/npcs/mordechai-redmoon-20-veteran', 1),
(65800, 1, 61152, 5, 1, 1, 1,  2.00, 'https://www.jabbithole.com/npcs/mordechai-redmoon-20-veteran', 1),
(65800, 1, 61139, 5, 1, 1, 1,  2.00, 'https://www.jabbithole.com/npcs/mordechai-redmoon-20-veteran', 1),
(65800, 1, 61141, 5, 1, 1, 1,  2.00, 'https://www.jabbithole.com/npcs/mordechai-redmoon-20-veteran', 1),
(65800, 1, 61153, 5, 1, 1, 1,  2.00, 'https://www.jabbithole.com/npcs/mordechai-redmoon-20-veteran', 1),
(65800, 1, 61145, 5, 1, 1, 1,  1.80, 'https://www.jabbithole.com/npcs/mordechai-redmoon-20-veteran', 1),
(65800, 1, 61147, 5, 1, 1, 1,  1.80, 'https://www.jabbithole.com/npcs/mordechai-redmoon-20-veteran', 1),
(65800, 1, 30559, 5, 1, 1, 1,  5.64, 'https://www.jabbithole.com/npcs/mordechai-redmoon-20-veteran', 1),
(65800, 1, 49629, 5, 1, 1, 1,  1.11, 'https://www.jabbithole.com/npcs/mordechai-redmoon-20-veteran', 1),
(65800, 1, 32924, 5, 1, 1, 1,  0.20, 'https://www.jabbithole.com/npcs/mordechai-redmoon-20-veteran', 1);
");

            // ── Laveka the Dark-Hearted (65997) — final boss ───────────────────────────
            migrationBuilder.Sql(@"
INSERT IGNORE INTO creature_loot_entry (creatureId, lootGroupId, itemId, context, sourceConfidence, minCount, maxCount, dropRate, evidenceRef, enabled) VALUES
(65997, 1, 74906, 5, 1, 1, 3, 100.00, 'Item2.tbl:74906 Marauder Insignia (final boss bonus)', 1),
(65997, 1, 42569, 5, 1, 1, 1,  18.36, 'Item2.tbl:42569 Partial Primal Pattern', 1),
(65997, 1, 38769, 5, 1, 1, 1,  12.00, 'Item2.tbl:38769 Tarnished Eldan Gift', 1),
(65997, 1, 49981, 5, 1, 1, 1,   6.25, 'Item2.tbl:49981 Eldan Runic Module', 1),
(65997, 1, 74945, 5, 1, 1, 1,  12.00, 'Item2.tbl:74945 Grimvoid Marauder Kit (final boss)', 1),
(65997, 1, 74952, 5, 1, 1, 1,  12.00, 'Item2.tbl:74952 Grimvoid Marauder Kit (final boss)', 1),
(65997, 1, 74959, 5, 1, 1, 1,  12.00, 'Item2.tbl:74959 Grimvoid Marauder Kit (final boss)', 1),
(65997, 1, 74900, 5, 1, 1, 1,   3.00, 'Item2.tbl:74900 Marauder Psyblade cosmetic', 1),
(65997, 1, 74901, 5, 1, 1, 1,   3.00, 'Item2.tbl:74901 Marauder Greatsword cosmetic', 1),
(65997, 1, 74902, 5, 1, 1, 1,   3.00, 'Item2.tbl:74902 Marauder Resonators cosmetic', 1),
(65997, 1, 74903, 5, 1, 1, 1,   3.00, 'Item2.tbl:74903 Marauder Launcher cosmetic', 1),
(65997, 1, 74904, 5, 1, 1, 1,   3.00, 'Item2.tbl:74904 Marauder Claws cosmetic', 1),
(65997, 1, 74905, 5, 1, 1, 1,   3.00, 'Item2.tbl:74905 Marauder Pistols cosmetic', 1),
(65997, 1, 73636, 5, 1, 1, 1,   2.50, 'Item2.tbl:73636 Marauder Helm cosmetic', 1),
(65997, 1, 73637, 5, 1, 1, 1,   2.50, 'Item2.tbl:73637 Marauder Chest cosmetic', 1),
(65997, 1, 73638, 5, 1, 1, 1,   2.50, 'Item2.tbl:73638 Marauder Pants cosmetic', 1),
(65997, 1, 73639, 5, 1, 1, 1,   2.50, 'Item2.tbl:73639 Marauder Shoulders cosmetic', 1),
(65997, 1, 73640, 5, 1, 1, 1,   2.50, 'Item2.tbl:73640 Marauder Gloves cosmetic', 1),
(65997, 1, 73641, 5, 1, 1, 1,   2.50, 'Item2.tbl:73641 Marauder Boots cosmetic', 1),
(65997, 1, 21896, 5, 1, 1, 1,   2.90, 'https://www.jabbithole.com/npcs/mordechai-redmoon-20-veteran', 1);
");

            // ═══════════════════════════════════════════════════════════════════════════
            // Augmentors' Lab (Raid, WorldId 3040) — 5 bosses (encounter e2681)
            //
            // Loot source: Item2.tbl client-data inference.
            // Augmentor weapon cosmetics (74914-74919) confirmed from Item2.tbl icon paths
            // Icon_ItemWeapon_Augmentor_*. No Jabbithole data exists for AugLab.
            // Augmentor Hoverboard fortune box (83661) from Icon_ItemMount_Hoverboard_Augmentor,
            // family=25/cat=138 (Fortune item type), Quality=5 (Superb).
            //
            // Generic raid tokens (42569, 38769) shared with other raid instances.
            // context=5 (Raid). sourceConfidence=1.
            // ═══════════════════════════════════════════════════════════════════════════

            // ── Augmenters God Unit (50979) — final boss ───────────────────────────────
            migrationBuilder.Sql(@"
INSERT IGNORE INTO creature_loot_entry (creatureId, lootGroupId, itemId, context, sourceConfidence, minCount, maxCount, dropRate, evidenceRef, enabled) VALUES
(50979, 1, 42569, 5, 1, 1, 1, 18.00, 'Item2.tbl:42569 Partial Primal Pattern', 1),
(50979, 1, 38769, 5, 1, 1, 1, 12.00, 'Item2.tbl:38769 Tarnished Eldan Gift', 1),
(50979, 1, 49981, 5, 1, 1, 1,  6.25, 'Item2.tbl:49981 Eldan Runic Module', 1),
(50979, 1, 83661, 5, 1, 1, 1, 15.00, 'Item2.tbl:83661 Augmentor Hoverboard Fortune Box', 1),
(50979, 1, 74914, 5, 1, 1, 1,  3.00, 'Item2.tbl:74914 Augmentor Psyblade cosmetic', 1),
(50979, 1, 74915, 5, 1, 1, 1,  3.00, 'Item2.tbl:74915 Augmentor Greatsword cosmetic', 1),
(50979, 1, 74916, 5, 1, 1, 1,  3.00, 'Item2.tbl:74916 Augmentor Resonators cosmetic', 1),
(50979, 1, 74917, 5, 1, 1, 1,  3.00, 'Item2.tbl:74917 Augmentor Launcher cosmetic', 1),
(50979, 1, 74918, 5, 1, 1, 1,  3.00, 'Item2.tbl:74918 Augmentor Claws cosmetic', 1),
(50979, 1, 74919, 5, 1, 1, 1,  3.00, 'Item2.tbl:74919 Augmentor Pistols cosmetic', 1),
(50979, 1, 53701, 5, 1, 1, 1,  1.00, 'Item2.tbl:53701 Augmented Jabbit pet', 1),
(50979, 1, 53739, 5, 1, 1, 1,  0.50, 'Item2.tbl:53739 Augmented Ravenok pet (Superb)', 1),
(50979, 1, 53782, 5, 1, 1, 1,  0.50, 'Item2.tbl:53782 Augmented Garr pet (Superb)', 1);
");

            // ── Prime Evolutionary Operant (50472) ─────────────────────────────────────
            migrationBuilder.Sql(@"
INSERT IGNORE INTO creature_loot_entry (creatureId, lootGroupId, itemId, context, sourceConfidence, minCount, maxCount, dropRate, evidenceRef, enabled) VALUES
(50472, 1, 42569, 5, 1, 1, 1, 15.00, 'Item2.tbl:42569 Partial Primal Pattern', 1),
(50472, 1, 38769, 5, 1, 1, 1, 10.00, 'Item2.tbl:38769 Tarnished Eldan Gift', 1),
(50472, 1, 49981, 5, 1, 1, 1,  5.00, 'Item2.tbl:49981 Eldan Runic Module', 1),
(50472, 1, 74914, 5, 1, 1, 1,  2.50, 'Item2.tbl:74914 Augmentor Psyblade cosmetic', 1),
(50472, 1, 74916, 5, 1, 1, 1,  2.50, 'Item2.tbl:74916 Augmentor Resonators cosmetic', 1),
(50472, 1, 53716, 5, 1, 1, 1,  1.00, 'Item2.tbl:53716 Augmented Chua pet', 1);
");

            // ── Phaged Evolutionary Operant (50423) ────────────────────────────────────
            migrationBuilder.Sql(@"
INSERT IGNORE INTO creature_loot_entry (creatureId, lootGroupId, itemId, context, sourceConfidence, minCount, maxCount, dropRate, evidenceRef, enabled) VALUES
(50423, 1, 42569, 5, 1, 1, 1, 15.00, 'Item2.tbl:42569 Partial Primal Pattern', 1),
(50423, 1, 38769, 5, 1, 1, 1, 10.00, 'Item2.tbl:38769 Tarnished Eldan Gift', 1),
(50423, 1, 49981, 5, 1, 1, 1,  5.00, 'Item2.tbl:49981 Eldan Runic Module', 1),
(50423, 1, 74915, 5, 1, 1, 1,  2.50, 'Item2.tbl:74915 Augmentor Greatsword cosmetic', 1),
(50423, 1, 74918, 5, 1, 1, 1,  2.50, 'Item2.tbl:74918 Augmentor Claws cosmetic', 1),
(50423, 1, 53729, 5, 1, 1, 1,  1.00, 'Item2.tbl:53729 Augmented Razortail pet', 1);
");

            // ── Chestacabra (50425) ────────────────────────────────────────────────────
            migrationBuilder.Sql(@"
INSERT IGNORE INTO creature_loot_entry (creatureId, lootGroupId, itemId, context, sourceConfidence, minCount, maxCount, dropRate, evidenceRef, enabled) VALUES
(50425, 1, 42569, 5, 1, 1, 1, 15.00, 'Item2.tbl:42569 Partial Primal Pattern', 1),
(50425, 1, 38769, 5, 1, 1, 1, 10.00, 'Item2.tbl:38769 Tarnished Eldan Gift', 1),
(50425, 1, 49981, 5, 1, 1, 1,  5.00, 'Item2.tbl:49981 Eldan Runic Module', 1),
(50425, 1, 74917, 5, 1, 1, 1,  2.50, 'Item2.tbl:74917 Augmentor Launcher cosmetic', 1),
(50425, 1, 74919, 5, 1, 1, 1,  2.50, 'Item2.tbl:74919 Augmentor Pistols cosmetic', 1),
(50425, 1, 53793, 5, 1, 1, 1,  1.00, 'Item2.tbl:53793 Augmented Buzzbing pet', 1);
");

            // ── Circuit Breaker (61597) ────────────────────────────────────────────────
            migrationBuilder.Sql(@"
INSERT IGNORE INTO creature_loot_entry (creatureId, lootGroupId, itemId, context, sourceConfidence, minCount, maxCount, dropRate, evidenceRef, enabled) VALUES
(61597, 1, 42569, 5, 1, 1, 1, 15.00, 'Item2.tbl:42569 Partial Primal Pattern', 1),
(61597, 1, 38769, 5, 1, 1, 1, 10.00, 'Item2.tbl:38769 Tarnished Eldan Gift', 1),
(61597, 1, 49981, 5, 1, 1, 1,  5.00, 'Item2.tbl:49981 Eldan Runic Module', 1),
(61597, 1, 83661, 5, 1, 1, 1,  8.00, 'Item2.tbl:83661 Augmentor Hoverboard Fortune Box', 1),
(61597, 1, 53856, 5, 1, 1, 1,  1.00, 'Item2.tbl:53856 Augmented Skeech pet', 1),
(61597, 1, 53868, 5, 1, 1, 1,  1.00, 'Item2.tbl:53868 Augmented Spider pet', 1);
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // RMT bosses
            migrationBuilder.Sql("DELETE FROM creature_loot_entry WHERE creatureId IN (65758, 65759, 65800, 65997, 66085);");
            // AugLab bosses
            migrationBuilder.Sql("DELETE FROM creature_loot_entry WHERE creatureId IN (50979, 50472, 50423, 50425, 61597);");
        }
    }
}
