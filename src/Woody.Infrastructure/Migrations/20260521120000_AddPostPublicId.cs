using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Woody.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPostPublicId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "public_id",
                table: "posts",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE posts
                SET public_id = 'pst_' || substr(replace(gen_random_uuid()::text, '-', ''), 1, 12)
                WHERE public_id IS NULL;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "public_id",
                table: "posts",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(16)",
                oldMaxLength: 16,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_posts_public_id",
                table: "posts",
                column: "public_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_posts_public_id",
                table: "posts");

            migrationBuilder.DropColumn(
                name: "public_id",
                table: "posts");
        }
    }
}
