using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexusForever.Database.Character.Migrations
{
    /// <inheritdoc />
    public partial class HousingUpkeep : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<uint>(
                name: "upkeep_charges",
                table: "residence_plot",
                type: "int",
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<float>(
                name: "upkeep_time",
                table: "residence_plot",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<uint[]>(
                name: "contribution_totals",
                table: "residence_plot",
                type: "nvarchar(max)",
                nullable: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "upkeep_charges", table: "residence_plot");
            migrationBuilder.DropColumn(name: "upkeep_time", table: "residence_plot");
            migrationBuilder.DropColumn(name: "contribution_totals", table: "residence_plot");
        }
    }
}
