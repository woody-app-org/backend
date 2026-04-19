using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Woody.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ExpandBillingSubscriptionsAndStripe : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_user_subscriptions_users_user_id",
                table: "user_subscriptions");

            migrationBuilder.DropPrimaryKey(
                name: "pk_user_subscriptions",
                table: "user_subscriptions");

            migrationBuilder.RenameTable(
                name: "user_subscriptions",
                newName: "subscriptions");

            migrationBuilder.RenameColumn(
                name: "external_subscription_id",
                table: "subscriptions",
                newName: "provider_subscription_id");

            migrationBuilder.RenameColumn(
                name: "external_customer_id",
                table: "subscriptions",
                newName: "provider_customer_id");

            migrationBuilder.AddColumn<int>(
                name: "billing_provider",
                table: "subscriptions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "plan_code",
                table: "subscriptions",
                type: "text",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE subscriptions
                SET plan_code = CASE plan
                    WHEN 0 THEN 'free'
                    WHEN 1 THEN 'pro_monthly'
                    ELSE 'free'
                END
                WHERE plan_code IS NULL;
                """);

            migrationBuilder.AddPrimaryKey(
                name: "pk_subscriptions",
                table: "subscriptions",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_subscriptions_provider_subscription_id",
                table: "subscriptions",
                column: "provider_subscription_id",
                unique: true,
                filter: "provider_subscription_id IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "fk_subscriptions_users_user_id",
                table: "subscriptions",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_subscriptions_users_user_id",
                table: "subscriptions");

            migrationBuilder.DropPrimaryKey(
                name: "pk_subscriptions",
                table: "subscriptions");

            migrationBuilder.DropIndex(
                name: "ix_subscriptions_provider_subscription_id",
                table: "subscriptions");

            migrationBuilder.DropColumn(
                name: "billing_provider",
                table: "subscriptions");

            migrationBuilder.DropColumn(
                name: "plan_code",
                table: "subscriptions");

            migrationBuilder.RenameTable(
                name: "subscriptions",
                newName: "user_subscriptions");

            migrationBuilder.RenameColumn(
                name: "provider_subscription_id",
                table: "user_subscriptions",
                newName: "external_subscription_id");

            migrationBuilder.RenameColumn(
                name: "provider_customer_id",
                table: "user_subscriptions",
                newName: "external_customer_id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_user_subscriptions",
                table: "user_subscriptions",
                column: "user_id");

            migrationBuilder.AddForeignKey(
                name: "fk_user_subscriptions_users_user_id",
                table: "user_subscriptions",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
