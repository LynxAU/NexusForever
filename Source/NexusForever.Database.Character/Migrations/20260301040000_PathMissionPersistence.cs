using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexusForever.Database.Character.Migrations
{
    public partial class PathMissionPersistence : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "character_path_mission",
                columns: table => new
                {
                    id        = table.Column<ulong>(type: "bigint(20) unsigned", nullable: false),
                    missionId = table.Column<uint>(type: "int(10) unsigned", nullable: false),
                    isCompleted = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    progress  = table.Column<uint>(type: "int(10) unsigned", nullable: false, defaultValue: 0u)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.id, x.missionId });
                    table.ForeignKey(
                        name: "FK__character_path_mission_id__character_id",
                        column: x => x.id,
                        principalTable: "character",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "character_path_explorer_node",
                columns: table => new
                {
                    id        = table.Column<ulong>(type: "bigint(20) unsigned", nullable: false),
                    missionId = table.Column<uint>(type: "int(10) unsigned", nullable: false),
                    nodeIndex = table.Column<uint>(type: "int(10) unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.id, x.missionId, x.nodeIndex });
                    table.ForeignKey(
                        name: "FK__character_path_explorer_node_id__character_id",
                        column: x => x.id,
                        principalTable: "character",
                        principalColumn: "id");
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "character_path_explorer_node");
            migrationBuilder.DropTable(name: "character_path_mission");
        }
    }
}
