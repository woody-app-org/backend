using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Woody.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemovePostTitle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "title",
                table: "posts");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "title",
                table: "posts",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
