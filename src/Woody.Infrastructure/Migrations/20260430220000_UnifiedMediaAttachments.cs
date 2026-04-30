using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Woody.Infrastructure.Migrations;

/// <summary>
/// Substitui <c>post_images</c> e <c>message_attachments</c> pela tabela unificada <c>media_attachments</c>
/// (<see cref="Woody.Domain.Entities.MediaAttachment"/>).
/// </summary>
public partial class UnifiedMediaAttachments : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE post_images ADD COLUMN IF NOT EXISTS media_kind integer NOT NULL DEFAULT 0;
            ALTER TABLE post_images ADD COLUMN IF NOT EXISTS mime_type text;
            ALTER TABLE post_images ADD COLUMN IF NOT EXISTS duration_seconds integer;
            ALTER TABLE message_attachments ADD COLUMN IF NOT EXISTS media_kind integer NOT NULL DEFAULT 0;
            ALTER TABLE message_attachments ADD COLUMN IF NOT EXISTS duration_seconds integer;
            """);

        migrationBuilder.Sql(
            """
            CREATE TABLE media_attachments (
                id SERIAL PRIMARY KEY,
                owner_type INTEGER NOT NULL,
                owner_id INTEGER NOT NULL,
                post_id INTEGER NULL,
                message_id INTEGER NULL,
                media_kind INTEGER NOT NULL DEFAULT 0,
                url TEXT NOT NULL,
                thumbnail_url TEXT NULL,
                mime_type TEXT NULL,
                file_size BIGINT NULL,
                width INTEGER NULL,
                height INTEGER NULL,
                duration_ms INTEGER NULL,
                provider TEXT NULL,
                external_id TEXT NULL,
                storage_key TEXT NULL,
                display_order INTEGER NOT NULL DEFAULT 0,
                created_at TIMESTAMPTZ NOT NULL,
                CONSTRAINT fk_media_attachments_posts_post_id FOREIGN KEY (post_id) REFERENCES posts(id) ON DELETE CASCADE,
                CONSTRAINT fk_media_attachments_messages_message_id FOREIGN KEY (message_id) REFERENCES messages(id) ON DELETE CASCADE,
                CONSTRAINT ck_media_attachments_owner_xor CHECK (
                    (owner_type = 1 AND post_id IS NOT NULL AND post_id = owner_id AND message_id IS NULL)
                    OR (owner_type = 2 AND message_id IS NOT NULL AND message_id = owner_id AND post_id IS NULL)
                )
            );

            CREATE INDEX ix_media_attachments_owner_type_owner_id_display_order
                ON media_attachments (owner_type, owner_id, display_order);

            CREATE INDEX ix_media_attachments_post_id ON media_attachments (post_id);
            CREATE INDEX ix_media_attachments_message_id ON media_attachments (message_id);
            """);

        migrationBuilder.Sql(
            """
            INSERT INTO media_attachments (
                owner_type, owner_id, post_id, message_id, media_kind, url, thumbnail_url, mime_type,
                file_size, width, height, duration_ms, provider, external_id, storage_key, display_order, created_at
            )
            SELECT
                1,
                pi.post_id,
                pi.post_id,
                NULL,
                COALESCE(pi.media_kind, 0),
                pi.url,
                NULL,
                pi.mime_type,
                NULL,
                NULL,
                NULL,
                CASE WHEN pi.duration_seconds IS NOT NULL THEN pi.duration_seconds * 1000 ELSE NULL END,
                NULL,
                NULL,
                pi.storage_key,
                pi.display_order,
                COALESCE(p.created_at, NOW() AT TIME ZONE 'utc')
            FROM post_images pi
            INNER JOIN posts p ON p.id = pi.post_id;
            """);

        migrationBuilder.Sql(
            """
            INSERT INTO media_attachments (
                owner_type, owner_id, post_id, message_id, media_kind, url, thumbnail_url, mime_type,
                file_size, width, height, duration_ms, provider, external_id, storage_key, display_order, created_at
            )
            SELECT
                2,
                ma.message_id,
                NULL,
                ma.message_id,
                COALESCE(ma.media_kind, 0),
                ma.url,
                NULL,
                ma.content_type,
                NULL,
                NULL,
                NULL,
                CASE WHEN ma.duration_seconds IS NOT NULL THEN ma.duration_seconds * 1000 ELSE NULL END,
                NULL,
                NULL,
                ma.storage_key,
                ma.display_order,
                ma.created_at
            FROM message_attachments ma;
            """);

        migrationBuilder.Sql(
            """
            DROP TABLE IF EXISTS message_attachments;
            DROP TABLE IF EXISTS post_images;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP TABLE IF EXISTS media_attachments;");
    }
}
