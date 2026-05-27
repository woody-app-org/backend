using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Woody.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "stories",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    author_user_id = table.Column<int>(type: "integer", nullable: false),
                    media_type = table.Column<int>(type: "integer", nullable: false),
                    media_url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    thumbnail_url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    storage_key = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    text = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    background_color = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    visibility = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    music_provider = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    music_track_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    music_title = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    music_artist = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    music_preview_url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stories", x => x.id);
                    table.ForeignKey(
                        name: "fk_stories_users_author_user_id",
                        column: x => x.author_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "story_views",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    story_id = table.Column<int>(type: "integer", nullable: false),
                    viewer_user_id = table.Column<int>(type: "integer", nullable: false),
                    viewed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_story_views", x => x.id);
                    table.ForeignKey(
                        name: "fk_story_views_stories_story_id",
                        column: x => x.story_id,
                        principalTable: "stories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_story_views_users_viewer_user_id",
                        column: x => x.viewer_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_stories_author_expires_not_deleted",
                table: "stories",
                columns: new[] { "author_user_id", "expires_at" },
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_stories_expires_at",
                table: "stories",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "ix_story_views_viewed_at",
                table: "story_views",
                column: "viewed_at");

            migrationBuilder.CreateIndex(
                name: "ix_story_views_viewer_user_id",
                table: "story_views",
                column: "viewer_user_id");

            migrationBuilder.CreateIndex(
                name: "ux_story_views_story_id_viewer_user_id",
                table: "story_views",
                columns: new[] { "story_id", "viewer_user_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "story_views");

            migrationBuilder.DropTable(
                name: "stories");
        }
    }
}
