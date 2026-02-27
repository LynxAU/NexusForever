using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexusForever.Database.World.Migrations
{
    /// <inheritdoc />
    public partial class FixMapEntranceWorldLocations : Migration
    {
        // Corrects expedition spawn worldLocationIds to match verified game-sniff data
        // from the NexusForever.WorldDatabase reference repository.
        //
        // Previous values were estimated; these are the authoritative IDs:
        //   Infestation (1232)            15568   (was 45843)
        //   Outpost M-13 (1319)           16970   (was 16869)
        //   RageLogic (1627)              24958   (was 38286)
        //   Space Madness (2149)          37466   (was 37708)
        //   Gauntlet (2183)               38838   (was 39001)
        //   Deep Space Exploration (2188) 38906   (was 38904)
        //   Fragment Zero (3180)          48725   (was 48374)
        //   Evil From The Ether (3404)    50210   (was 50622)
        //   Slaughterdome T0/T1 (1535)    23498/23499 (unchanged — already correct)
        //
        // Also adds the missing Cryo-Plex arena:
        //   The Cryo-Plex (3022) T0       45345
        //   The Cryo-Plex (3022) T1       45346

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "map_entrance",
                keyColumns: new[] { "mapId", "team" },
                keyValues: new object[] { (uint)1232, (byte)0 },
                column: "worldLocationId",
                value: (uint)15568);

            migrationBuilder.UpdateData(
                table: "map_entrance",
                keyColumns: new[] { "mapId", "team" },
                keyValues: new object[] { (uint)1319, (byte)0 },
                column: "worldLocationId",
                value: (uint)16970);

            migrationBuilder.UpdateData(
                table: "map_entrance",
                keyColumns: new[] { "mapId", "team" },
                keyValues: new object[] { (uint)1627, (byte)0 },
                column: "worldLocationId",
                value: (uint)24958);

            migrationBuilder.UpdateData(
                table: "map_entrance",
                keyColumns: new[] { "mapId", "team" },
                keyValues: new object[] { (uint)2149, (byte)0 },
                column: "worldLocationId",
                value: (uint)37466);

            migrationBuilder.UpdateData(
                table: "map_entrance",
                keyColumns: new[] { "mapId", "team" },
                keyValues: new object[] { (uint)2183, (byte)0 },
                column: "worldLocationId",
                value: (uint)38838);

            migrationBuilder.UpdateData(
                table: "map_entrance",
                keyColumns: new[] { "mapId", "team" },
                keyValues: new object[] { (uint)2188, (byte)0 },
                column: "worldLocationId",
                value: (uint)38906);

            migrationBuilder.UpdateData(
                table: "map_entrance",
                keyColumns: new[] { "mapId", "team" },
                keyValues: new object[] { (uint)3180, (byte)0 },
                column: "worldLocationId",
                value: (uint)48725);

            migrationBuilder.UpdateData(
                table: "map_entrance",
                keyColumns: new[] { "mapId", "team" },
                keyValues: new object[] { (uint)3404, (byte)0 },
                column: "worldLocationId",
                value: (uint)50210);

            // Add The Cryo-Plex arena (was missing from initial seed)
            migrationBuilder.InsertData(
                table: "map_entrance",
                columns: new[] { "mapId", "team", "worldLocationId" },
                values: new object[,]
                {
                    { (uint)3022, (byte)0, (uint)45345 },
                    { (uint)3022, (byte)1, (uint)45346 },
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "map_entrance",
                keyColumns: new[] { "mapId", "team" },
                keyValues: new object[] { (uint)1232, (byte)0 },
                column: "worldLocationId",
                value: (uint)45843);

            migrationBuilder.UpdateData(
                table: "map_entrance",
                keyColumns: new[] { "mapId", "team" },
                keyValues: new object[] { (uint)1319, (byte)0 },
                column: "worldLocationId",
                value: (uint)16869);

            migrationBuilder.UpdateData(
                table: "map_entrance",
                keyColumns: new[] { "mapId", "team" },
                keyValues: new object[] { (uint)1627, (byte)0 },
                column: "worldLocationId",
                value: (uint)38286);

            migrationBuilder.UpdateData(
                table: "map_entrance",
                keyColumns: new[] { "mapId", "team" },
                keyValues: new object[] { (uint)2149, (byte)0 },
                column: "worldLocationId",
                value: (uint)37708);

            migrationBuilder.UpdateData(
                table: "map_entrance",
                keyColumns: new[] { "mapId", "team" },
                keyValues: new object[] { (uint)2183, (byte)0 },
                column: "worldLocationId",
                value: (uint)39001);

            migrationBuilder.UpdateData(
                table: "map_entrance",
                keyColumns: new[] { "mapId", "team" },
                keyValues: new object[] { (uint)2188, (byte)0 },
                column: "worldLocationId",
                value: (uint)38904);

            migrationBuilder.UpdateData(
                table: "map_entrance",
                keyColumns: new[] { "mapId", "team" },
                keyValues: new object[] { (uint)3180, (byte)0 },
                column: "worldLocationId",
                value: (uint)48374);

            migrationBuilder.UpdateData(
                table: "map_entrance",
                keyColumns: new[] { "mapId", "team" },
                keyValues: new object[] { (uint)3404, (byte)0 },
                column: "worldLocationId",
                value: (uint)50622);

            migrationBuilder.DeleteData(table: "map_entrance", keyColumns: new[] { "mapId", "team" }, keyValues: new object[] { (uint)3022, (byte)0 });
            migrationBuilder.DeleteData(table: "map_entrance", keyColumns: new[] { "mapId", "team" }, keyValues: new object[] { (uint)3022, (byte)1 });
        }
    }
}
