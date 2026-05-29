using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Woody.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMessageSharedPost : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "shared_post_id",
                table: "messages",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_messages_shared_post_id",
                table: "messages",
                column: "shared_post_id");

            migrationBuilder.AddForeignKey(
                name: "fk_messages_posts_shared_post_id",
                table: "messages",
                column: "shared_post_id",
                principalTable: "posts",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_messages_posts_shared_post_id",
                table: "messages");

            migrationBuilder.DropIndex(
                name: "ix_messages_shared_post_id",
                table: "messages");

            migrationBuilder.DropColumn(
                name: "shared_post_id",
                table: "messages");
        }
    }
}
