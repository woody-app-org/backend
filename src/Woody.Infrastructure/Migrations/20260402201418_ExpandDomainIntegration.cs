using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Woody.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ExpandDomainIntegration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "birth_date",
                table: "users",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "cpf",
                table: "users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "display_name",
                table: "users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "location",
                table: "users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at",
                table: "posts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "image_url",
                table: "posts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "title",
                table: "posts",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "posts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at",
                table: "comments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "hidden_by_post_author_at",
                table: "comments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "communities",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    slug = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    category = table.Column<string>(type: "text", nullable: false),
                    rules = table.Column<string>(type: "text", nullable: false),
                    visibility = table.Column<string>(type: "text", nullable: false),
                    owner_user_id = table.Column<int>(type: "integer", nullable: false),
                    avatar_url = table.Column<string>(type: "text", nullable: true),
                    cover_url = table.Column<string>(type: "text", nullable: true),
                    member_count = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_communities", x => x.id);
                    table.ForeignKey(
                        name: "fk_communities_users_owner_user_id",
                        column: x => x.owner_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "content_reports",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    reporter_user_id = table.Column<int>(type: "integer", nullable: false),
                    target_type = table.Column<string>(type: "text", nullable: false),
                    post_id = table.Column<int>(type: "integer", nullable: true),
                    comment_id = table.Column<int>(type: "integer", nullable: true),
                    reason_code = table.Column<string>(type: "text", nullable: false),
                    details = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_content_reports", x => x.id);
                    table.ForeignKey(
                        name: "fk_content_reports_comments_comment_id",
                        column: x => x.comment_id,
                        principalTable: "comments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_content_reports_posts_post_id",
                        column: x => x.post_id,
                        principalTable: "posts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_content_reports_users_reporter_user_id",
                        column: x => x.reporter_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "post_tags",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    post_id = table.Column<int>(type: "integer", nullable: false),
                    tag = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_post_tags", x => x.id);
                    table.ForeignKey(
                        name: "fk_post_tags_posts_post_id",
                        column: x => x.post_id,
                        principalTable: "posts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_interests",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    label = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_interests", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_interests_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_social_links",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    platform = table.Column<string>(type: "text", nullable: false),
                    label = table.Column<string>(type: "text", nullable: false),
                    url = table.Column<string>(type: "text", nullable: false),
                    handle = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_social_links", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_social_links_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "community_memberships",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    community_id = table.Column<int>(type: "integer", nullable: false),
                    role = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    joined_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_community_memberships", x => x.id);
                    table.ForeignKey(
                        name: "fk_community_memberships_communities_community_id",
                        column: x => x.community_id,
                        principalTable: "communities",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_community_memberships_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "community_tags",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    community_id = table.Column<int>(type: "integer", nullable: false),
                    tag = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_community_tags", x => x.id);
                    table.ForeignKey(
                        name: "fk_community_tags_communities_community_id",
                        column: x => x.community_id,
                        principalTable: "communities",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "join_requests",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    community_id = table.Column<int>(type: "integer", nullable: false),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    requested_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_join_requests", x => x.id);
                    table.ForeignKey(
                        name: "fk_join_requests_communities_community_id",
                        column: x => x.community_id,
                        principalTable: "communities",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_join_requests_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql(
                """
                INSERT INTO communities (slug, name, description, category, rules, visibility, owner_user_id, avatar_url, cover_url, member_count, created_at)
                SELECT 'geral', 'Geral', 'Espaço geral da plataforma', 'outro', '', 'public', (SELECT MIN(id) FROM users), NULL, NULL, 0, NOW()
                WHERE NOT EXISTS (SELECT 1 FROM communities WHERE slug = 'geral')
                  AND EXISTS (SELECT 1 FROM users LIMIT 1);
                """);

            migrationBuilder.AddColumn<int>(
                name: "community_id",
                table: "posts",
                type: "integer",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE posts SET community_id = (SELECT id FROM communities ORDER BY id LIMIT 1)
                WHERE community_id IS NULL;
                """);

            migrationBuilder.AlterColumn<int>(
                name: "community_id",
                table: "posts",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_users_email",
                table: "users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_users_username",
                table: "users",
                column: "username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_posts_community_id",
                table: "posts",
                column: "community_id");

            migrationBuilder.CreateIndex(
                name: "ix_communities_owner_user_id",
                table: "communities",
                column: "owner_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_communities_slug",
                table: "communities",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_community_memberships_community_id",
                table: "community_memberships",
                column: "community_id");

            migrationBuilder.CreateIndex(
                name: "ix_community_memberships_user_id_community_id",
                table: "community_memberships",
                columns: new[] { "user_id", "community_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_community_tags_community_id",
                table: "community_tags",
                column: "community_id");

            migrationBuilder.CreateIndex(
                name: "ix_content_reports_comment_id",
                table: "content_reports",
                column: "comment_id");

            migrationBuilder.CreateIndex(
                name: "ix_content_reports_post_id",
                table: "content_reports",
                column: "post_id");

            migrationBuilder.CreateIndex(
                name: "ix_content_reports_reporter_user_id",
                table: "content_reports",
                column: "reporter_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_join_requests_community_id_user_id_status",
                table: "join_requests",
                columns: new[] { "community_id", "user_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_join_requests_user_id",
                table: "join_requests",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_post_tags_post_id",
                table: "post_tags",
                column: "post_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_interests_user_id",
                table: "user_interests",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_social_links_user_id",
                table: "user_social_links",
                column: "user_id");

            migrationBuilder.AddForeignKey(
                name: "fk_posts_communities_community_id",
                table: "posts",
                column: "community_id",
                principalTable: "communities",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_posts_communities_community_id",
                table: "posts");

            migrationBuilder.DropTable(
                name: "community_memberships");

            migrationBuilder.DropTable(
                name: "community_tags");

            migrationBuilder.DropTable(
                name: "content_reports");

            migrationBuilder.DropTable(
                name: "join_requests");

            migrationBuilder.DropTable(
                name: "post_tags");

            migrationBuilder.DropTable(
                name: "user_interests");

            migrationBuilder.DropTable(
                name: "user_social_links");

            migrationBuilder.DropTable(
                name: "communities");

            migrationBuilder.DropIndex(
                name: "ix_users_email",
                table: "users");

            migrationBuilder.DropIndex(
                name: "ix_users_username",
                table: "users");

            migrationBuilder.DropIndex(
                name: "ix_posts_community_id",
                table: "posts");

            migrationBuilder.DropColumn(
                name: "birth_date",
                table: "users");

            migrationBuilder.DropColumn(
                name: "cpf",
                table: "users");

            migrationBuilder.DropColumn(
                name: "display_name",
                table: "users");

            migrationBuilder.DropColumn(
                name: "location",
                table: "users");

            migrationBuilder.DropColumn(
                name: "community_id",
                table: "posts");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "posts");

            migrationBuilder.DropColumn(
                name: "image_url",
                table: "posts");

            migrationBuilder.DropColumn(
                name: "title",
                table: "posts");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "posts");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "comments");

            migrationBuilder.DropColumn(
                name: "hidden_by_post_author_at",
                table: "comments");
        }
    }
}
