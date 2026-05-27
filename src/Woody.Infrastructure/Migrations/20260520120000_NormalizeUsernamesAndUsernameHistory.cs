using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Woody.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class NormalizeUsernamesAndUsernameHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "username_history",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", Npgsql.EntityFrameworkCore.PostgreSQL.Metadata.NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    old_username = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    new_username = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    changed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_username_history", x => x.id);
                    table.ForeignKey(
                        name: "fk_username_history_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_username_history_old_username",
                table: "username_history",
                column: "old_username");

            migrationBuilder.CreateIndex(
                name: "ix_username_history_user_id",
                table: "username_history",
                column: "user_id");

            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                  IF EXISTS (
                    SELECT 1
                    FROM (
                      SELECT lower(trim(username)) AS normalized, count(*) AS cnt
                      FROM users
                      GROUP BY lower(trim(username))
                      HAVING count(*) > 1
                    ) AS duplicates
                  ) THEN
                    RAISE EXCEPTION 'Migration aborted: usernames conflict when lowercased. Resolve duplicates manually.';
                  END IF;
                END $$;
                """);

            migrationBuilder.Sql(
                """
                UPDATE users
                SET username = regexp_replace(
                    replace(lower(trim(username)), '-', '_'),
                    '[^a-z0-9_.]', '', 'g')
                WHERE username IS NOT NULL;
                """);

            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                  IF EXISTS (
                    SELECT 1 FROM users
                    WHERE char_length(username) < 3 OR char_length(username) > 30
                       OR username !~ '^[a-z0-9_.]+$'
                  ) THEN
                    RAISE EXCEPTION 'Migration aborted: invalid username length or characters after normalization.';
                  END IF;
                END $$;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "username",
                table: "users",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "username",
                table: "users",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(30)",
                oldMaxLength: 30);

            migrationBuilder.DropTable(
                name: "username_history");
        }
    }
}
