using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Woody.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPreLaunchSignups : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "pre_launch_signups",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    social_network = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    social_username = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    normalized_social_network = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    normalized_social_username = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    accepted_contact_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    source = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pre_launch_signups", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ux_pre_launch_signups_network_username",
                table: "pre_launch_signups",
                columns: new[] { "normalized_social_network", "normalized_social_username" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "pre_launch_signups");
        }
    }
}
