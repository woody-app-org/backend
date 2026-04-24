using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Woody.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCommunitySubscriptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "community_subscriptions",
                columns: table => new
                {
                    community_id = table.Column<int>(type: "integer", nullable: false),
                    plan = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    plan_code = table.Column<string>(type: "text", nullable: true),
                    billing_provider = table.Column<int>(type: "integer", nullable: false),
                    current_period_start = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    current_period_end = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    cancel_at_period_end = table.Column<bool>(type: "boolean", nullable: false),
                    provider_customer_id = table.Column<string>(type: "text", nullable: true),
                    provider_subscription_id = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_community_subscriptions", x => x.community_id);
                    table.ForeignKey(
                        name: "fk_community_subscriptions_communities_community_id",
                        column: x => x.community_id,
                        principalTable: "communities",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_community_subscriptions_provider_subscription_id",
                table: "community_subscriptions",
                column: "provider_subscription_id",
                unique: true,
                filter: "provider_subscription_id IS NOT NULL");

            migrationBuilder.Sql(
                """
                INSERT INTO community_subscriptions (
                    community_id, plan, status, plan_code, billing_provider,
                    current_period_start, current_period_end, cancel_at_period_end,
                    provider_customer_id, provider_subscription_id, created_at, updated_at)
                SELECT c.id, 0, 0, 'community_free', 0,
                    NULL, NULL, false,
                    NULL, NULL, NOW() AT TIME ZONE 'UTC', NULL
                FROM communities c
                WHERE NOT EXISTS (
                    SELECT 1 FROM community_subscriptions s WHERE s.community_id = c.id);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "community_subscriptions");
        }
    }
}
