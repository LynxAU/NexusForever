using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexusForever.Database.World.Migrations
{
    /// <inheritdoc />
    public partial class MapEntranceSeed : Migration
    {
        // WorldLocation2 entry IDs used as dungeon/instance entrance spawn positions.
        // Coordinates verified from WorldLocation2.tbl game table binary.
        //
        // Expeditions (team = 0):
        //   Infestation (1232)            → WL2 45843  (  0.00, -500.00,     0.00)
        //   Outpost M-13 (1319)           → WL2 16869  (  0.00, -446.70,     0.00)
        //   RageLogic (1627)              → WL2 38286  ( -0.94, -816.19,   -37.84)
        //   Space Madness (2149)          → WL2 37708  ( -0.84,    0.08,    72.15)
        //   Gauntlet (2183)               → WL2 39001  (-1007.08,  0.00,  1063.78)
        //   Deep Space Exploration (2188) → WL2 38904  (-1048.20, -18.94, 1025.66)
        //   Fragment Zero (3180)          → WL2 48374  (9888.87, -782.99, -6096.78)
        //   Evil From The Ether (3404)    → WL2 50622  ( -53.51, -844.94,   118.81)
        //
        // Arena (teams 0 and 1):
        //   The Slaughterdome (1535) T0   → WL2 23498  (  0.05, -894.52,   -52.29)
        //   The Slaughterdome (1535) T1   → WL2 23499  ( -0.47, -894.50,    50.43)
        //
        // Battleground (teams 0 and 1):
        //   Walatiki Temple (797) T0      → WL2 7188   (487.11, -514.76,     1.61)
        //   Walatiki Temple (797) T1      → WL2 7189   (659.62, -514.58,   257.83)

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Expedition entrances (single team, team = 0)
            migrationBuilder.InsertData(
                table: "map_entrance",
                columns: new[] { "mapId", "team", "worldLocationId" },
                values: new object[,]
                {
                    // Infestation
                    { (uint)1232, (byte)0, (uint)45843 },
                    // Outpost M-13
                    { (uint)1319, (byte)0, (uint)16869 },
                    // RageLogic
                    { (uint)1627, (byte)0, (uint)38286 },
                    // Space Madness
                    { (uint)2149, (byte)0, (uint)37708 },
                    // Gauntlet
                    { (uint)2183, (byte)0, (uint)39001 },
                    // Deep Space Exploration
                    { (uint)2188, (byte)0, (uint)38904 },
                    // Fragment Zero
                    { (uint)3180, (byte)0, (uint)48374 },
                    // Evil From The Ether
                    { (uint)3404, (byte)0, (uint)50622 },
                });

            // The Slaughterdome arena (team 0 = Blue/first team, team 1 = Red/second team)
            migrationBuilder.InsertData(
                table: "map_entrance",
                columns: new[] { "mapId", "team", "worldLocationId" },
                values: new object[,]
                {
                    { (uint)1535, (byte)0, (uint)23498 },
                    { (uint)1535, (byte)1, (uint)23499 },
                });

            // Walatiki Temple battleground (team 0 = Exile side, team 1 = Dominion side)
            migrationBuilder.InsertData(
                table: "map_entrance",
                columns: new[] { "mapId", "team", "worldLocationId" },
                values: new object[,]
                {
                    { (uint)797, (byte)0, (uint)7188 },
                    { (uint)797, (byte)1, (uint)7189 },
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(table: "map_entrance", keyColumns: new[] { "mapId", "team" }, keyValues: new object[] { (uint)1232, (byte)0 });
            migrationBuilder.DeleteData(table: "map_entrance", keyColumns: new[] { "mapId", "team" }, keyValues: new object[] { (uint)1319, (byte)0 });
            migrationBuilder.DeleteData(table: "map_entrance", keyColumns: new[] { "mapId", "team" }, keyValues: new object[] { (uint)1627, (byte)0 });
            migrationBuilder.DeleteData(table: "map_entrance", keyColumns: new[] { "mapId", "team" }, keyValues: new object[] { (uint)2149, (byte)0 });
            migrationBuilder.DeleteData(table: "map_entrance", keyColumns: new[] { "mapId", "team" }, keyValues: new object[] { (uint)2183, (byte)0 });
            migrationBuilder.DeleteData(table: "map_entrance", keyColumns: new[] { "mapId", "team" }, keyValues: new object[] { (uint)2188, (byte)0 });
            migrationBuilder.DeleteData(table: "map_entrance", keyColumns: new[] { "mapId", "team" }, keyValues: new object[] { (uint)3180, (byte)0 });
            migrationBuilder.DeleteData(table: "map_entrance", keyColumns: new[] { "mapId", "team" }, keyValues: new object[] { (uint)3404, (byte)0 });
            migrationBuilder.DeleteData(table: "map_entrance", keyColumns: new[] { "mapId", "team" }, keyValues: new object[] { (uint)1535, (byte)0 });
            migrationBuilder.DeleteData(table: "map_entrance", keyColumns: new[] { "mapId", "team" }, keyValues: new object[] { (uint)1535, (byte)1 });
            migrationBuilder.DeleteData(table: "map_entrance", keyColumns: new[] { "mapId", "team" }, keyValues: new object[] { (uint)797,  (byte)0 });
            migrationBuilder.DeleteData(table: "map_entrance", keyColumns: new[] { "mapId", "team" }, keyValues: new object[] { (uint)797,  (byte)1 });
        }
    }
}
