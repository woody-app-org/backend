using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Woody.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProfileAndCommentPins : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_posts_user_id",
                table: "posts");

            migrationBuilder.DropIndex(
                name: "ix_comments_post_id",
                table: "comments");

            migrationBuilder.AddColumn<DateTime>(
                name: "pinned_on_profile_at",
                table: "posts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "pinned_on_post_at",
                table: "comments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_posts_user_id_pinned_on_profile_at",
                table: "posts",
                columns: new[] { "user_id", "pinned_on_profile_at" });

            migrationBuilder.CreateIndex(
                name: "ix_comments_post_id",
                table: "comments",
                column: "post_id",
                unique: true,
                filter: "pinned_on_post_at IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_posts_user_id_pinned_on_profile_at",
                table: "posts");

            migrationBuilder.DropIndex(
                name: "ix_comments_post_id",
                table: "comments");

            migrationBuilder.DropColumn(
                name: "pinned_on_profile_at",
                table: "posts");

            migrationBuilder.DropColumn(
                name: "pinned_on_post_at",
                table: "comments");

            migrationBuilder.CreateIndex(
                name: "ix_posts_user_id",
                table: "posts",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_comments_post_id",
                table: "comments",
                column: "post_id");
        }
    }
}
