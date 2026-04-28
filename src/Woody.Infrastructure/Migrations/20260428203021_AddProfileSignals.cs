using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Woody.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProfileSignals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "profile_signals",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    sender_user_id = table.Column<int>(type: "integer", nullable: false),
                    receiver_user_id = table.Column<int>(type: "integer", nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    message = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    read_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    archived_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    dismissed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_profile_signals", x => x.id);
                    table.ForeignKey(
                        name: "fk_profile_signals_users_receiver_user_id",
                        column: x => x.receiver_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_profile_signals_users_sender_user_id",
                        column: x => x.sender_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_profile_signals_created_at",
                table: "profile_signals",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_profile_signals_receiver_user_id",
                table: "profile_signals",
                column: "receiver_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_profile_signals_receiver_user_id_status_created_at",
                table: "profile_signals",
                columns: new[] { "receiver_user_id", "status", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_profile_signals_sender_user_id",
                table: "profile_signals",
                column: "sender_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_profile_signals_sender_user_id_created_at",
                table: "profile_signals",
                columns: new[] { "sender_user_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_profile_signals_sender_user_id_receiver_user_id_type_create",
                table: "profile_signals",
                columns: new[] { "sender_user_id", "receiver_user_id", "type", "created_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "profile_signals");
        }
    }
}
