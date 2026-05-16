using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Woody.Infrastructure.Migrations;

/// <summary>Anexos multimédia: tipo semântico, MIME e duração (vídeo).</summary>
public partial class AddPostAndMessageMediaKinds : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "media_kind",
            table: "post_images",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<string>(
            name: "mime_type",
            table: "post_images",
            type: "text",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "duration_seconds",
            table: "post_images",
            type: "integer",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "media_kind",
            table: "message_attachments",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "duration_seconds",
            table: "message_attachments",
            type: "integer",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "duration_seconds", table: "message_attachments");
        migrationBuilder.DropColumn(name: "media_kind", table: "message_attachments");
        migrationBuilder.DropColumn(name: "duration_seconds", table: "post_images");
        migrationBuilder.DropColumn(name: "mime_type", table: "post_images");
        migrationBuilder.DropColumn(name: "media_kind", table: "post_images");
    }
}
