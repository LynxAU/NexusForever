using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexusForever.Database.Character.Migrations
{
    public partial class GuildRecruitmentMetadata : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<uint>(
                name: "classification",
                table: "guild_guild_data",
                type: "int(10) unsigned",
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "recruitmentDemand",
                table: "guild_guild_data",
                type: "int(10) unsigned",
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<string>(
                name: "recruitmentDescription",
                table: "guild_guild_data",
                type: "varchar(500)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<uint>(
                name: "recruitmentMinLevel",
                table: "guild_guild_data",
                type: "int(10) unsigned",
                nullable: false,
                defaultValue: 1u);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "classification",
                table: "guild_guild_data");

            migrationBuilder.DropColumn(
                name: "recruitmentDemand",
                table: "guild_guild_data");

            migrationBuilder.DropColumn(
                name: "recruitmentDescription",
                table: "guild_guild_data");

            migrationBuilder.DropColumn(
                name: "recruitmentMinLevel",
                table: "guild_guild_data");
        }
    }
}
