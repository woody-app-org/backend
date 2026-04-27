using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Woody.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAuthSessionsAndLoginLockouts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "login_lockouts",
                columns: table => new
                {
                    normalized_login = table.Column<string>(type: "text", nullable: false),
                    failed_attempt_count = table.Column<int>(type: "integer", nullable: false),
                    first_failed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_failed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    lockout_end_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_login_lockouts", x => x.normalized_login);
                });

            migrationBuilder.CreateTable(
                name: "refresh_token_sessions",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    token_hash = table.Column<string>(type: "text", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    revoked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    replaced_by_token_hash = table.Column<string>(type: "text", nullable: true),
                    revocation_reason = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_refresh_token_sessions", x => x.id);
                    table.ForeignKey(
                        name: "fk_refresh_token_sessions_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_login_lockouts_lockout_end_at",
                table: "login_lockouts",
                column: "lockout_end_at");

            migrationBuilder.CreateIndex(
                name: "ix_refresh_token_sessions_token_hash",
                table: "refresh_token_sessions",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_refresh_token_sessions_user_id_expires_at",
                table: "refresh_token_sessions",
                columns: new[] { "user_id", "expires_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "login_lockouts");

            migrationBuilder.DropTable(
                name: "refresh_token_sessions");
        }
    }
}
