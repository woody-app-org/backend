using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using Woody.Infrastructure.Persistence.Context;

#nullable disable

namespace Woody.Infrastructure.Migrations;

/// <summary>Tabela de convites para beta fechado e associação opcional em <c>users</c>.</summary>
[DbContext(typeof(WoodyDbContext))]
[Migration("20260507120000_AddBetaInvites")]
public partial class AddBetaInvites : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "beta_invites",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                code = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                label = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                max_uses = table.Column<int>(type: "integer", nullable: false),
                uses_count = table.Column<int>(type: "integer", nullable: false),
                expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                is_active = table.Column<bool>(type: "boolean", nullable: false),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                created_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_beta_invites", x => x.id);
                table.CheckConstraint("ck_beta_invites_max_uses_positive", "max_uses > 0");
            });

        migrationBuilder.CreateIndex(
            name: "ix_beta_invites_code",
            table: "beta_invites",
            column: "code",
            unique: true);

        migrationBuilder.AddColumn<int>(
            name: "invite_id",
            table: "users",
            type: "integer",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "ix_users_invite_id",
            table: "users",
            column: "invite_id");

        migrationBuilder.AddForeignKey(
            name: "fk_users_beta_invites_invite_id",
            table: "users",
            column: "invite_id",
            principalTable: "beta_invites",
            principalColumn: "id",
            onDelete: ReferentialAction.SetNull);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "fk_users_beta_invites_invite_id",
            table: "users");

        migrationBuilder.DropIndex(
            name: "ix_users_invite_id",
            table: "users");

        migrationBuilder.DropColumn(
            name: "invite_id",
            table: "users");

        migrationBuilder.DropTable(
            name: "beta_invites");
    }
}
