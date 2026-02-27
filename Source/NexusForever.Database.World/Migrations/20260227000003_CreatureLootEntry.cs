using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexusForever.Database.World.Migrations
{
    /// <inheritdoc />
    public partial class CreatureLootEntry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "creature_loot_entry",
                columns: table => new
                {
                    creatureId = table.Column<uint>(type: "int(10) unsigned", nullable: false, defaultValue: 0u),
                    lootGroupId = table.Column<uint>(type: "int(10) unsigned", nullable: false, defaultValue: 0u),
                    itemId = table.Column<uint>(type: "int(10) unsigned", nullable: false, defaultValue: 0u),
                    context = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false, defaultValue: (byte)0),
                    sourceConfidence = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false, defaultValue: (byte)0),
                    minCount = table.Column<uint>(type: "int(10) unsigned", nullable: false, defaultValue: 1u),
                    maxCount = table.Column<uint>(type: "int(10) unsigned", nullable: false, defaultValue: 1u),
                    dropRate = table.Column<float>(type: "float", nullable: false, defaultValue: 1f),
                    evidenceRef = table.Column<string>(type: "varchar(255)", nullable: false, defaultValue: ""),
                    enabled = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.creatureId, x.lootGroupId, x.itemId, x.context, x.sourceConfidence });
                });

            migrationBuilder.CreateIndex(
                name: "ix_creature_loot_entry_creature_context_enabled",
                table: "creature_loot_entry",
                columns: new[] { "creatureId", "context", "enabled" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "creature_loot_entry");
        }
    }
}
