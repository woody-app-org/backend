using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Woody.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddContentReportReviewFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "internal_note",
                table: "content_reports",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "resolution_code",
                table: "content_reports",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "reviewed_at",
                table: "content_reports",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "reviewed_by_user_id",
                table: "content_reports",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "status",
                table: "content_reports",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "content_reports",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_content_reports_reviewed_by_user_id",
                table: "content_reports",
                column: "reviewed_by_user_id");

            migrationBuilder.AddForeignKey(
                name: "fk_content_reports_users_reviewed_by_user_id",
                table: "content_reports",
                column: "reviewed_by_user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_content_reports_users_reviewed_by_user_id",
                table: "content_reports");

            migrationBuilder.DropIndex(
                name: "ix_content_reports_reviewed_by_user_id",
                table: "content_reports");

            migrationBuilder.DropColumn(
                name: "internal_note",
                table: "content_reports");

            migrationBuilder.DropColumn(
                name: "resolution_code",
                table: "content_reports");

            migrationBuilder.DropColumn(
                name: "reviewed_at",
                table: "content_reports");

            migrationBuilder.DropColumn(
                name: "reviewed_by_user_id",
                table: "content_reports");

            migrationBuilder.DropColumn(
                name: "status",
                table: "content_reports");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "content_reports");
        }
    }
}
