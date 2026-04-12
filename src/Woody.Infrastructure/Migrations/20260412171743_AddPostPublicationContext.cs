using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Woody.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPostPublicationContext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "community_id",
                table: "posts",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<int>(
                name: "publication_context",
                table: "posts",
                type: "integer",
                nullable: false,
                defaultValue: 2);

            migrationBuilder.CreateIndex(
                name: "ix_posts_publication_context_user_id",
                table: "posts",
                columns: new[] { "publication_context", "user_id" });

            migrationBuilder.AddCheckConstraint(
                name: "ck_posts_publication_context_community",
                table: "posts",
                sql: "(publication_context = 2 AND community_id IS NOT NULL) OR (publication_context = 1 AND community_id IS NULL)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_posts_publication_context_user_id",
                table: "posts");

            migrationBuilder.DropCheckConstraint(
                name: "ck_posts_publication_context_community",
                table: "posts");

            migrationBuilder.DropColumn(
                name: "publication_context",
                table: "posts");

            migrationBuilder.AlterColumn<int>(
                name: "community_id",
                table: "posts",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);
        }
    }
}
