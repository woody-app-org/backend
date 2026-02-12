using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Woody.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddingTopicsAndRelations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "topics",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_topics", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "post_topics",
                columns: table => new
                {
                    post_id = table.Column<int>(type: "integer", nullable: false),
                    topic_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_post_topics", x => new { x.post_id, x.topic_id });
                    table.ForeignKey(
                        name: "fk_post_topics_posts_post_id",
                        column: x => x.post_id,
                        principalTable: "posts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_post_topics_topics_topic_id",
                        column: x => x.topic_id,
                        principalTable: "topics",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_topics",
                columns: table => new
                {
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    topic_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_topics", x => new { x.user_id, x.topic_id });
                    table.ForeignKey(
                        name: "fk_user_topics_topics_topic_id",
                        column: x => x.topic_id,
                        principalTable: "topics",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_user_topics_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_post_topics_topic_id",
                table: "post_topics",
                column: "topic_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_topics_topic_id",
                table: "user_topics",
                column: "topic_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "post_topics");

            migrationBuilder.DropTable(
                name: "user_topics");

            migrationBuilder.DropTable(
                name: "topics");
        }
    }
}
