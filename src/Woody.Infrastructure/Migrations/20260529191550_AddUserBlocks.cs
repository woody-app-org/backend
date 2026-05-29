using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Woody.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserBlocks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "user_blocks",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    blocker_user_id = table.Column<int>(type: "integer", nullable: false),
                    blocked_user_id = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_blocks", x => x.id);
                    table.CheckConstraint("ck_user_blocks_not_self", "blocker_user_id <> blocked_user_id");
                    table.ForeignKey(
                        name: "fk_user_blocks_users_blocked_user_id",
                        column: x => x.blocked_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_user_blocks_users_blocker_user_id",
                        column: x => x.blocker_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_user_blocks_blocked_user_id",
                table: "user_blocks",
                column: "blocked_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_blocks_blocker_user_id",
                table: "user_blocks",
                column: "blocker_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_blocks_blocker_user_id_blocked_user_id",
                table: "user_blocks",
                columns: new[] { "blocker_user_id", "blocked_user_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_blocks");
        }
    }
}
