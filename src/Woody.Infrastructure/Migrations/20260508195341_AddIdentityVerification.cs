using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Woody.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIdentityVerification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "verification_status",
                table: "users",
                type: "text",
                nullable: false,
                defaultValue: "PendingDocument");

            migrationBuilder.CreateTable(
                name: "identity_verifications",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false, defaultValue: "PendingDocument"),
                    document_storage_key = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    document_submitted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    reviewed_by_user_id = table.Column<int>(type: "integer", nullable: true),
                    reviewed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    rejection_reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    attempt_count = table.Column<int>(type: "integer", nullable: false),
                    consent_given_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    document_deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    decision_log = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_identity_verifications", x => x.id);
                    table.ForeignKey(
                        name: "fk_identity_verifications_users_reviewed_by_user_id",
                        column: x => x.reviewed_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_identity_verifications_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_identity_verifications_reviewed_by_user_id",
                table: "identity_verifications",
                column: "reviewed_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_identity_verifications_user_id",
                table: "identity_verifications",
                column: "user_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "identity_verifications");

            migrationBuilder.DropColumn(
                name: "verification_status",
                table: "users");
        }
    }
}
