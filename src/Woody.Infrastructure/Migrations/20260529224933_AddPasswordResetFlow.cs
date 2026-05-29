using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Woody.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPasswordResetFlow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_email_verification_codes_email_created_at",
                table: "email_verification_codes");

            migrationBuilder.AddColumn<int>(
                name: "purpose",
                table: "email_verification_codes",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "password_reset_sessions",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    token_hash = table.Column<string>(type: "text", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    consumed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_password_reset_sessions", x => x.id);
                    table.ForeignKey(
                        name: "fk_password_reset_sessions_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_email_verification_codes_email_purpose_created_at",
                table: "email_verification_codes",
                columns: new[] { "email", "purpose", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_password_reset_sessions_token_hash",
                table: "password_reset_sessions",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_password_reset_sessions_user_id_expires_at",
                table: "password_reset_sessions",
                columns: new[] { "user_id", "expires_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "password_reset_sessions");

            migrationBuilder.DropIndex(
                name: "ix_email_verification_codes_email_purpose_created_at",
                table: "email_verification_codes");

            migrationBuilder.DropColumn(
                name: "purpose",
                table: "email_verification_codes");

            migrationBuilder.CreateIndex(
                name: "ix_email_verification_codes_email_created_at",
                table: "email_verification_codes",
                columns: new[] { "email", "created_at" });
        }
    }
}
