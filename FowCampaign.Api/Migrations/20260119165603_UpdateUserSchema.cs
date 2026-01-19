using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FowCampaign.Api.Migrations
{
    /// <inheritdoc />
    public partial class UpdateUserSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Color",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Role",
                table: "Users");

            migrationBuilder.RenameColumn(
                name: "isTurn",
                table: "CampaignPlayer",
                newName: "IsTurn");

            migrationBuilder.RenameColumn(
                name: "isAlive",
                table: "CampaignPlayer",
                newName: "IsAlive");

            migrationBuilder.RenameColumn(
                name: "factionName",
                table: "CampaignPlayer",
                newName: "FactionName");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "IsTurn",
                table: "CampaignPlayer",
                newName: "isTurn");

            migrationBuilder.RenameColumn(
                name: "IsAlive",
                table: "CampaignPlayer",
                newName: "isAlive");

            migrationBuilder.RenameColumn(
                name: "FactionName",
                table: "CampaignPlayer",
                newName: "factionName");

            migrationBuilder.AddColumn<string>(
                name: "Color",
                table: "Users",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Role",
                table: "Users",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }
    }
}
