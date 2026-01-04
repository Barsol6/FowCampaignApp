using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FowCampaign.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddMultiplayerEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MapDataJson",
                table: "Campaigns");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "Campaigns",
                newName: "MapFileName");

            migrationBuilder.RenameColumn(
                name: "OwnerUsername",
                table: "Campaigns",
                newName: "JoinCode");

            migrationBuilder.RenameColumn(
                name: "LastPlayed",
                table: "Campaigns",
                newName: "GameStateJson");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Campaigns",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "OwnerId",
                table: "Campaigns",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "CampaignPlayer",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CampaignId = table.Column<int>(type: "INTEGER", nullable: false),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    factionName = table.Column<string>(type: "TEXT", nullable: false),
                    isAlive = table.Column<bool>(type: "INTEGER", nullable: false),
                    isTurn = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CampaignPlayer", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CampaignPlayer_Campaigns_CampaignId",
                        column: x => x.CampaignId,
                        principalTable: "Campaigns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CampaignPlayer_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CampaignPlayer_CampaignId",
                table: "CampaignPlayer",
                column: "CampaignId");

            migrationBuilder.CreateIndex(
                name: "IX_CampaignPlayer_UserId",
                table: "CampaignPlayer",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CampaignPlayer");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Campaigns");

            migrationBuilder.DropColumn(
                name: "OwnerId",
                table: "Campaigns");

            migrationBuilder.RenameColumn(
                name: "MapFileName",
                table: "Campaigns",
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "JoinCode",
                table: "Campaigns",
                newName: "OwnerUsername");

            migrationBuilder.RenameColumn(
                name: "GameStateJson",
                table: "Campaigns",
                newName: "LastPlayed");

            migrationBuilder.AddColumn<string>(
                name: "MapDataJson",
                table: "Campaigns",
                type: "TEXT",
                nullable: true);
        }
    }
}
