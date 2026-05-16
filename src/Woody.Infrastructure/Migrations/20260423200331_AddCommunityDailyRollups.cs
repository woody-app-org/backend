using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Woody.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCommunityDailyRollups : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "community_daily_rollups",
                columns: table => new
                {
                    community_id = table.Column<int>(type: "integer", nullable: false),
                    day_utc = table.Column<DateOnly>(type: "date", nullable: false),
                    page_views = table.Column<int>(type: "integer", nullable: false),
                    member_leaves = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_community_daily_rollups", x => new { x.community_id, x.day_utc });
                    table.ForeignKey(
                        name: "fk_community_daily_rollups_communities_community_id",
                        column: x => x.community_id,
                        principalTable: "communities",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_community_daily_rollups_community_id",
                table: "community_daily_rollups",
                column: "community_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "community_daily_rollups");
        }
    }
}
