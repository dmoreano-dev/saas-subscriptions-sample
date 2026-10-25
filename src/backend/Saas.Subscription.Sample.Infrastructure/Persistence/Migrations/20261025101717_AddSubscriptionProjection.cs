using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Saas.Subscription.Sample.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSubscriptionProjection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "subscriptions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    plan_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    current_period_start = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    current_period_end = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    access_until = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ended_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_subscriptions", x => x.id);
                    table.UniqueConstraint("ak_subscriptions_account_id_id", x => new { x.account_id, x.id });
                    table.CheckConstraint("ck_subscriptions_access_until", "access_until > current_period_start");
                    table.CheckConstraint("ck_subscriptions_ended_at", "(status = 'Ended') = (ended_at IS NOT NULL)");
                    table.CheckConstraint("ck_subscriptions_period", "current_period_end > current_period_start");
                    table.CheckConstraint("ck_subscriptions_status", "status IN ('Active', 'PastDue', 'Ended')");
                    table.ForeignKey(
                        name: "fk_subscriptions_accounts_account_id",
                        column: x => x.account_id,
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_subscriptions_plan_versions_plan_version_id",
                        column: x => x.plan_version_id,
                        principalTable: "plan_versions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "subscription_changes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    subscription_id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    target_plan_version_id = table.Column<Guid>(type: "uuid", nullable: true),
                    effective_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    requested_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    resolved_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_subscription_changes", x => x.id);
                    table.CheckConstraint("ck_subscription_changes_kind", "kind IN ('PlanChange', 'Cancel')");
                    table.CheckConstraint("ck_subscription_changes_resolved_at", "(status = 'Pending') = (resolved_at IS NULL)");
                    table.CheckConstraint("ck_subscription_changes_status", "status IN ('Pending', 'Applied', 'Cancelled')");
                    table.CheckConstraint("ck_subscription_changes_target", "(kind = 'PlanChange') = (target_plan_version_id IS NOT NULL)");
                    table.ForeignKey(
                        name: "fk_subscription_changes_plan_versions_target_plan_version_id",
                        column: x => x.target_plan_version_id,
                        principalTable: "plan_versions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_subscription_changes_subscriptions_account_id_subscription_",
                        columns: x => new { x.account_id, x.subscription_id },
                        principalTable: "subscriptions",
                        principalColumns: new[] { "account_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_subscription_changes_account_id_subscription_id",
                table: "subscription_changes",
                columns: new[] { "account_id", "subscription_id" });

            migrationBuilder.CreateIndex(
                name: "ix_subscription_changes_target_plan_version_id",
                table: "subscription_changes",
                column: "target_plan_version_id");

            migrationBuilder.CreateIndex(
                name: "ux_subscription_changes_subscription_id_pending",
                table: "subscription_changes",
                column: "subscription_id",
                unique: true,
                filter: "status = 'Pending'");

            migrationBuilder.CreateIndex(
                name: "ix_subscriptions_plan_version_id",
                table: "subscriptions",
                column: "plan_version_id");

            migrationBuilder.CreateIndex(
                name: "ux_subscriptions_account_id_current",
                table: "subscriptions",
                column: "account_id",
                unique: true,
                filter: "status <> 'Ended'");

            // A subscription or a scheduled change may reference only a published plan version: a draft can still
            // change, and a subscriber's capabilities must not. The column to check is the trigger argument.
            // 55000 = object_not_in_prerequisite_state, as for the catalog immutability triggers.
            migrationBuilder.Sql("""
                CREATE FUNCTION subscriptions_guard_plan_version() RETURNS trigger LANGUAGE plpgsql AS $$
                DECLARE
                    version_id uuid := (to_jsonb(NEW) ->> TG_ARGV[0])::uuid;
                BEGIN
                    IF version_id IS NOT NULL AND NOT EXISTS (
                        SELECT 1 FROM plan_versions WHERE id = version_id AND status = 'Published') THEN
                        RAISE EXCEPTION 'plan version % is not published', version_id
                            USING ERRCODE = '55000';
                    END IF;
                    RETURN NEW;
                END $$;

                CREATE TRIGGER trg_subscriptions_published_plan_version
                    BEFORE INSERT OR UPDATE OF plan_version_id ON subscriptions
                    FOR EACH ROW EXECUTE FUNCTION subscriptions_guard_plan_version('plan_version_id');

                CREATE TRIGGER trg_subscription_changes_published_plan_version
                    BEFORE INSERT OR UPDATE OF target_plan_version_id ON subscription_changes
                    FOR EACH ROW EXECUTE FUNCTION subscriptions_guard_plan_version('target_plan_version_id');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "subscription_changes");

            migrationBuilder.DropTable(
                name: "subscriptions");

            migrationBuilder.Sql("DROP FUNCTION subscriptions_guard_plan_version();");
        }
    }
}
