using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Woody.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCommentOptionalGifFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "gif_external_id",
                table: "comments",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "gif_provider",
                table: "comments",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "gif_thumbnail_url",
                table: "comments",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "gif_title",
                table: "comments",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "gif_url",
                table: "comments",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "gif_external_id",
                table: "comments");

            migrationBuilder.DropColumn(
                name: "gif_provider",
                table: "comments");

            migrationBuilder.DropColumn(
                name: "gif_thumbnail_url",
                table: "comments");

            migrationBuilder.DropColumn(
                name: "gif_title",
                table: "comments");

            migrationBuilder.DropColumn(
                name: "gif_url",
                table: "comments");
        }
    }
}
