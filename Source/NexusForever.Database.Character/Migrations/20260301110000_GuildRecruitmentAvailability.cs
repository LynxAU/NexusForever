using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexusForever.Database.Character.Migrations
{
    public partial class GuildRecruitmentAvailability : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<uint>(
                name: "recruitmentAvailability",
                table: "guild_member",
                type: "int(10) unsigned",
                nullable: false,
                defaultValue: 0u);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "recruitmentAvailability",
                table: "guild_member");
        }
    }
}
