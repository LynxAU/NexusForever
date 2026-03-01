using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexusForever.Database.Character.Migrations
{
    public partial class GuildPerksAndBankTabs : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "activePerksJson",
                table: "guild_guild_data",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "bankTabNamesJson",
                table: "guild_guild_data",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "unlockedPerksJson",
                table: "guild_guild_data",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "activePerksJson",
                table: "guild_guild_data");

            migrationBuilder.DropColumn(
                name: "bankTabNamesJson",
                table: "guild_guild_data");

            migrationBuilder.DropColumn(
                name: "unlockedPerksJson",
                table: "guild_guild_data");
        }
    }
}
