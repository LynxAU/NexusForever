using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexusForever.Database.World.Migrations
{
    /// <inheritdoc />
    public partial class DungeonAdventureRaidEntranceSeed : Migration
    {
        // WorldLocation2 (WL2) entry IDs used as entrance spawn positions for instanced content.
        // WL2 IDs were derived from WorldLocation2.tbl / World.tbl game table binaries.
        // Entrance selection heuristic: minimum Euclidean distance to map origin.
        // Coordinates are approximate — correct with sniff data when available.
        //
        // DUNGEONS (World.tbl MapType=11, team=0):
        //   Ruins of Kel Voreth / EthnDunon (382)         → WL2   1286  (   3.69,    8.95,    -7.28)
        //   Skullcano Island (1263)                        → WL2  19570  (multiple candidates ~dist 985)
        //   Sanctuary of the Swordmaiden / Torine (1271)  → WL2  33721  (dist ~4795)
        //   Stormtalon's Lair / OsunDungeon (1336)        → WL2  18557  (  61.21, -854.33,    84.63)
        //   Crimelabs 4K / InfiniteLabs (2980)             → WL2  51076  (dist ~12815)
        //   Hall of the Hundred (3009)                     → WL2  48578  (dist ~714)
        //   Protogames Academy Juniors (3173)              → WL2  50175  (dist ~33566)
        //   Coldblood Citadel (3522)                       → WL2  53227  (dist ~376)
        //
        // ADVENTURES (World.tbl MapType=6, team=0):
        //   Hycrest Insurrection (1149)                    → WL2  45920  (dist ~2721)
        //   Malgrave Trail (1181)                          → WL2  14931  (dist ~2395)
        //   Galeras Holdout (1233)                         → WL2  38164  (dist ~5610)
        //   Whitevale Offensive (1323)                     → WL2  32601  (dist ~4277)
        //   Northern Wilds Adventure (1393)                → WL2  36239  (dist ~6623)
        //   Star-Comm Station / AstrovoidPrison (1437)     → WL2  40797  (dist ~494)
        //   Farside Adventure (3010)                       → WL2  48540  (dist ~6588)
        //   Levian Bay Adventure (3176)                    → WL2  49018  (dist ~6119)
        //
        // RAIDS (World.tbl MapType=9, team=0):
        //   Datascape (1333)                               → WL2  19279  (   0.00,   32.11,     0.00)
        //   Genetic Archives (1462)                        → WL2  43421  (dist ~226)
        //   Red Moon Terror 20-man (3032)                  → WL2  49183  (dist ~43)
        //   The Augmentors' Lab (3040)                     → WL2  47530  (dist ~1737)
        //   Ultimate Protogames (3041)                     → WL2  46197  (dist ~34831)
        //   Red Moon Terror 40-man (3102)                  → WL2  49252  (dist ~1274)

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── Dungeons ──────────────────────────────────────────────────────────────
            migrationBuilder.InsertData(
                table: "map_entrance",
                columns: new[] { "mapId", "team", "worldLocationId" },
                values: new object[,]
                {
                    // Ruins of Kel Voreth (EthnDunon)
                    { (uint)382,  (byte)0, (uint)1286  },
                    // Skullcano Island
                    { (uint)1263, (byte)0, (uint)19570 },
                    // Sanctuary of the Swordmaiden (TorineDungeon)
                    { (uint)1271, (byte)0, (uint)33721 },
                    // Stormtalon's Lair (OsunDungeon)
                    { (uint)1336, (byte)0, (uint)18557 },
                    // Crimelabs 4K (InfiniteLabs)
                    { (uint)2980, (byte)0, (uint)51076 },
                    // Hall of the Hundred
                    { (uint)3009, (byte)0, (uint)48578 },
                    // Protogames Academy Juniors
                    { (uint)3173, (byte)0, (uint)50175 },
                    // Coldblood Citadel
                    { (uint)3522, (byte)0, (uint)53227 },
                });

            // ── Adventures ───────────────────────────────────────────────────────────
            migrationBuilder.InsertData(
                table: "map_entrance",
                columns: new[] { "mapId", "team", "worldLocationId" },
                values: new object[,]
                {
                    // Hycrest Insurrection
                    { (uint)1149, (byte)0, (uint)45920 },
                    // Malgrave Trail
                    { (uint)1181, (byte)0, (uint)14931 },
                    // Galeras Holdout
                    { (uint)1233, (byte)0, (uint)38164 },
                    // Whitevale Offensive
                    { (uint)1323, (byte)0, (uint)32601 },
                    // Northern Wilds Adventure
                    { (uint)1393, (byte)0, (uint)36239 },
                    // Star-Comm Station (AstrovoidPrison)
                    { (uint)1437, (byte)0, (uint)40797 },
                    // Farside Adventure
                    { (uint)3010, (byte)0, (uint)48540 },
                    // Levian Bay Adventure
                    { (uint)3176, (byte)0, (uint)49018 },
                });

            // ── Raids ─────────────────────────────────────────────────────────────────
            migrationBuilder.InsertData(
                table: "map_entrance",
                columns: new[] { "mapId", "team", "worldLocationId" },
                values: new object[,]
                {
                    // Datascape (20-player)
                    { (uint)1333, (byte)0, (uint)19279 },
                    // Genetic Archives (20-player)
                    { (uint)1462, (byte)0, (uint)43421 },
                    // Red Moon Terror 20-man
                    { (uint)3032, (byte)0, (uint)49183 },
                    // The Augmentors' Lab
                    { (uint)3040, (byte)0, (uint)47530 },
                    // Ultimate Protogames (40-player)
                    { (uint)3041, (byte)0, (uint)46197 },
                    // Red Moon Terror 40-man
                    { (uint)3102, (byte)0, (uint)49252 },
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Dungeons
            migrationBuilder.DeleteData("map_entrance", new[] { "mapId", "team" }, new object[] { (uint)382,  (byte)0 });
            migrationBuilder.DeleteData("map_entrance", new[] { "mapId", "team" }, new object[] { (uint)1263, (byte)0 });
            migrationBuilder.DeleteData("map_entrance", new[] { "mapId", "team" }, new object[] { (uint)1271, (byte)0 });
            migrationBuilder.DeleteData("map_entrance", new[] { "mapId", "team" }, new object[] { (uint)1336, (byte)0 });
            migrationBuilder.DeleteData("map_entrance", new[] { "mapId", "team" }, new object[] { (uint)2980, (byte)0 });
            migrationBuilder.DeleteData("map_entrance", new[] { "mapId", "team" }, new object[] { (uint)3009, (byte)0 });
            migrationBuilder.DeleteData("map_entrance", new[] { "mapId", "team" }, new object[] { (uint)3173, (byte)0 });
            migrationBuilder.DeleteData("map_entrance", new[] { "mapId", "team" }, new object[] { (uint)3522, (byte)0 });
            // Adventures
            migrationBuilder.DeleteData("map_entrance", new[] { "mapId", "team" }, new object[] { (uint)1149, (byte)0 });
            migrationBuilder.DeleteData("map_entrance", new[] { "mapId", "team" }, new object[] { (uint)1181, (byte)0 });
            migrationBuilder.DeleteData("map_entrance", new[] { "mapId", "team" }, new object[] { (uint)1233, (byte)0 });
            migrationBuilder.DeleteData("map_entrance", new[] { "mapId", "team" }, new object[] { (uint)1323, (byte)0 });
            migrationBuilder.DeleteData("map_entrance", new[] { "mapId", "team" }, new object[] { (uint)1393, (byte)0 });
            migrationBuilder.DeleteData("map_entrance", new[] { "mapId", "team" }, new object[] { (uint)1437, (byte)0 });
            migrationBuilder.DeleteData("map_entrance", new[] { "mapId", "team" }, new object[] { (uint)3010, (byte)0 });
            migrationBuilder.DeleteData("map_entrance", new[] { "mapId", "team" }, new object[] { (uint)3176, (byte)0 });
            // Raids
            migrationBuilder.DeleteData("map_entrance", new[] { "mapId", "team" }, new object[] { (uint)1333, (byte)0 });
            migrationBuilder.DeleteData("map_entrance", new[] { "mapId", "team" }, new object[] { (uint)1462, (byte)0 });
            migrationBuilder.DeleteData("map_entrance", new[] { "mapId", "team" }, new object[] { (uint)3032, (byte)0 });
            migrationBuilder.DeleteData("map_entrance", new[] { "mapId", "team" }, new object[] { (uint)3040, (byte)0 });
            migrationBuilder.DeleteData("map_entrance", new[] { "mapId", "team" }, new object[] { (uint)3041, (byte)0 });
            migrationBuilder.DeleteData("map_entrance", new[] { "mapId", "team" }, new object[] { (uint)3102, (byte)0 });
        }
    }
}
