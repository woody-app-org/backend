using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Woody.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SupportPreRegistrationEmailVerification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_email_verification_codes_users_user_id",
                table: "email_verification_codes");

            migrationBuilder.DropIndex(
                name: "ix_email_verification_codes_user_id_created_at",
                table: "email_verification_codes");

            migrationBuilder.AlterColumn<int>(
                name: "user_id",
                table: "email_verification_codes",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<string>(
                name: "email",
                table: "email_verification_codes",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "ix_email_verification_codes_email_created_at",
                table: "email_verification_codes",
                columns: new[] { "email", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_email_verification_codes_user_id",
                table: "email_verification_codes",
                column: "user_id");

            migrationBuilder.AddForeignKey(
                name: "fk_email_verification_codes_users_user_id",
                table: "email_verification_codes",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_email_verification_codes_users_user_id",
                table: "email_verification_codes");

            migrationBuilder.DropIndex(
                name: "ix_email_verification_codes_email_created_at",
                table: "email_verification_codes");

            migrationBuilder.DropIndex(
                name: "ix_email_verification_codes_user_id",
                table: "email_verification_codes");

            migrationBuilder.DropColumn(
                name: "email",
                table: "email_verification_codes");

            migrationBuilder.AlterColumn<int>(
                name: "user_id",
                table: "email_verification_codes",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_email_verification_codes_user_id_created_at",
                table: "email_verification_codes",
                columns: new[] { "user_id", "created_at" });

            migrationBuilder.AddForeignKey(
                name: "fk_email_verification_codes_users_user_id",
                table: "email_verification_codes",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
