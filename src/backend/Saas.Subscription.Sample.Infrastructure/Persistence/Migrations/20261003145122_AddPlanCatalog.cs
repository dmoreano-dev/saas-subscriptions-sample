using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Saas.Subscription.Sample.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPlanCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "feature_definitions",
                columns: table => new
                {
                    key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    value_type = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    unit = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_feature_definitions", x => x.key);
                    table.CheckConstraint("ck_feature_definitions_limit_has_unit", "(value_type = 'Limit') = (unit IS NOT NULL)");
                    table.CheckConstraint("ck_feature_definitions_unit", "unit IS NULL OR unit IN ('Projects', 'Reports', 'Seats')");
                    table.CheckConstraint("ck_feature_definitions_value_type", "value_type IN ('Boolean', 'Limit')");
                });

            migrationBuilder.CreateTable(
                name: "plans",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    key = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    account_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    is_billable = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_plans", x => x.id);
                    table.CheckConstraint("ck_plans_account_type", "account_type IN ('Personal', 'Organization')");
                });

            migrationBuilder.CreateTable(
                name: "plan_versions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    plan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    published_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_plan_versions", x => x.id);
                    table.CheckConstraint("ck_plan_versions_published_at", "(status = 'Published') = (published_at IS NOT NULL)");
                    table.CheckConstraint("ck_plan_versions_status", "status IN ('Draft', 'Published')");
                    table.CheckConstraint("ck_plan_versions_version", "version >= 1");
                    table.ForeignKey(
                        name: "fk_plan_versions_plans_plan_id",
                        column: x => x.plan_id,
                        principalTable: "plans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "plan_capabilities",
                columns: table => new
                {
                    plan_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    feature_key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    bool_value = table.Column<bool>(type: "boolean", nullable: true),
                    limit_value = table.Column<int>(type: "integer", nullable: true),
                    is_unlimited = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_plan_capabilities", x => new { x.plan_version_id, x.feature_key });
                    table.CheckConstraint("ck_plan_capabilities_limit_value", "limit_value IS NULL OR limit_value >= 0");
                    table.CheckConstraint("ck_plan_capabilities_one_value", "(bool_value IS NOT NULL)::int + (limit_value IS NOT NULL)::int + is_unlimited::int = 1");
                    table.ForeignKey(
                        name: "fk_plan_capabilities_feature_definitions_feature_key",
                        column: x => x.feature_key,
                        principalTable: "feature_definitions",
                        principalColumn: "key",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_plan_capabilities_plan_versions_plan_version_id",
                        column: x => x.plan_version_id,
                        principalTable: "plan_versions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "plan_prices",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    plan_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    interval = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    currency = table.Column<string>(type: "character(3)", fixedLength: true, maxLength: 3, nullable: false),
                    unit_amount_minor = table.Column<int>(type: "integer", nullable: false),
                    external_price_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_plan_prices", x => x.id);
                    table.CheckConstraint("ck_plan_prices_currency", "currency ~ '^[A-Z]{3}$'");
                    table.CheckConstraint("ck_plan_prices_external_price_id", "external_price_id IS NULL OR external_price_id <> ''");
                    table.CheckConstraint("ck_plan_prices_interval", "interval IN ('Month', 'Year')");
                    table.CheckConstraint("ck_plan_prices_unit_amount_minor", "unit_amount_minor > 0");
                    table.ForeignKey(
                        name: "fk_plan_prices_plan_versions_plan_version_id",
                        column: x => x.plan_version_id,
                        principalTable: "plan_versions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_plan_capabilities_feature_key",
                table: "plan_capabilities",
                column: "feature_key");

            migrationBuilder.CreateIndex(
                name: "ux_plan_prices_plan_version_id_interval_currency",
                table: "plan_prices",
                columns: new[] { "plan_version_id", "interval", "currency" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_plan_versions_plan_id_version",
                table: "plan_versions",
                columns: new[] { "plan_id", "version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_plans_key",
                table: "plans",
                column: "key",
                unique: true);

            // Published-version immutability lives in PostgreSQL so no code path can bypass it.
            // 55000 = object_not_in_prerequisite_state (the PostgreSQL error code the tests assert on).
            migrationBuilder.Sql("""
                CREATE FUNCTION catalog_guard_plan_version() RETURNS trigger LANGUAGE plpgsql AS $$
                BEGIN
                    -- A published version never changes or disappears; Draft -> Published is the only transition.
                    IF OLD.status = 'Published' THEN
                        RAISE EXCEPTION 'plan version % is published and immutable', OLD.id
                            USING ERRCODE = '55000';
                    END IF;
                    IF TG_OP = 'DELETE' THEN
                        RETURN OLD;
                    END IF;
                    RETURN NEW;
                END $$;

                CREATE TRIGGER trg_plan_versions_immutable
                    BEFORE UPDATE OR DELETE ON plan_versions
                    FOR EACH ROW EXECUTE FUNCTION catalog_guard_plan_version();

                CREATE FUNCTION catalog_guard_plan_version_child() RETURNS trigger LANGUAGE plpgsql AS $$
                DECLARE
                    version_id uuid;
                BEGIN
                    -- Rows of a published version cannot be added, changed, moved or removed.
                    FOREACH version_id IN ARRAY ARRAY[
                        CASE WHEN TG_OP <> 'INSERT' THEN OLD.plan_version_id END,
                        CASE WHEN TG_OP <> 'DELETE' THEN NEW.plan_version_id END]
                    LOOP
                        IF version_id IS NOT NULL AND EXISTS (
                            SELECT 1 FROM plan_versions WHERE id = version_id AND status = 'Published') THEN
                            RAISE EXCEPTION 'plan version % is published and immutable', version_id
                                USING ERRCODE = '55000';
                        END IF;
                    END LOOP;
                    IF TG_OP = 'DELETE' THEN
                        RETURN OLD;
                    END IF;
                    RETURN NEW;
                END $$;

                CREATE TRIGGER trg_plan_capabilities_immutable
                    BEFORE INSERT OR UPDATE OR DELETE ON plan_capabilities
                    FOR EACH ROW EXECUTE FUNCTION catalog_guard_plan_version_child();

                CREATE TRIGGER trg_plan_prices_immutable
                    BEFORE INSERT OR UPDATE OR DELETE ON plan_prices
                    FOR EACH ROW EXECUTE FUNCTION catalog_guard_plan_version_child();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "plan_capabilities");

            migrationBuilder.DropTable(
                name: "plan_prices");

            migrationBuilder.DropTable(
                name: "feature_definitions");

            migrationBuilder.DropTable(
                name: "plan_versions");

            migrationBuilder.DropTable(
                name: "plans");

            migrationBuilder.Sql("""
                DROP FUNCTION catalog_guard_plan_version();
                DROP FUNCTION catalog_guard_plan_version_child();
                """);
        }
    }
}
