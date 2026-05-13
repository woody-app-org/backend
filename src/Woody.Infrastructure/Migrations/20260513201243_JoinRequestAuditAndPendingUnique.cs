using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Woody.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class JoinRequestAuditAndPendingUnique : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_join_requests_community_id_user_id_status",
                table: "join_requests");

            migrationBuilder.AddColumn<string>(
                name: "rejection_reason",
                table: "join_requests",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "reviewed_at",
                table: "join_requests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "reviewed_by_user_id",
                table: "join_requests",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "join_requests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_join_requests_reviewed_by_user_id",
                table: "join_requests",
                column: "reviewed_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ux_join_requests_community_user_pending",
                table: "join_requests",
                columns: new[] { "community_id", "user_id" },
                unique: true,
                filter: "status = 'pending'");

            migrationBuilder.AddForeignKey(
                name: "fk_join_requests_users_reviewed_by_user_id",
                table: "join_requests",
                column: "reviewed_by_user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_join_requests_users_reviewed_by_user_id",
                table: "join_requests");

            migrationBuilder.DropIndex(
                name: "ix_join_requests_reviewed_by_user_id",
                table: "join_requests");

            migrationBuilder.DropIndex(
                name: "ux_join_requests_community_user_pending",
                table: "join_requests");

            migrationBuilder.DropColumn(
                name: "rejection_reason",
                table: "join_requests");

            migrationBuilder.DropColumn(
                name: "reviewed_at",
                table: "join_requests");

            migrationBuilder.DropColumn(
                name: "reviewed_by_user_id",
                table: "join_requests");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "join_requests");

            migrationBuilder.CreateIndex(
                name: "ix_join_requests_community_id_user_id_status",
                table: "join_requests",
                columns: new[] { "community_id", "user_id", "status" });
        }
    }
}
