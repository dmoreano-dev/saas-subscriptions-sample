using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Saas.Subscription.Sample.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPermissionsAndProjects : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "memberships",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Active");

            migrationBuilder.CreateTable(
                name: "permissions",
                columns: table => new
                {
                    key = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    scope = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_permissions", x => x.key);
                    table.UniqueConstraint("ak_permissions_key_scope", x => new { x.key, x.scope });
                    table.CheckConstraint("ck_permissions_scope", "scope IN ('Account', 'Platform')");
                });

            migrationBuilder.CreateTable(
                name: "projects",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_projects", x => x.id);
                    table.UniqueConstraint("ak_projects_account_id_id", x => new { x.account_id, x.id });
                    table.ForeignKey(
                        name: "fk_projects_accounts_account_id",
                        column: x => x.account_id,
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "roles",
                columns: table => new
                {
                    key = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    scope = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_roles", x => x.key);
                    table.UniqueConstraint("ak_roles_key_scope", x => new { x.key, x.scope });
                    table.CheckConstraint("ck_roles_scope", "scope IN ('Account', 'Platform')");
                });

            migrationBuilder.CreateTable(
                name: "membership_roles",
                columns: table => new
                {
                    membership_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role_key = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    role_scope = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_membership_roles", x => new { x.membership_id, x.role_key });
                    table.CheckConstraint("ck_membership_roles_role_scope", "role_scope = 'Account'");
                    table.ForeignKey(
                        name: "fk_membership_roles_memberships_membership_id",
                        column: x => x.membership_id,
                        principalTable: "memberships",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_membership_roles_roles_role_key_role_scope",
                        columns: x => new { x.role_key, x.role_scope },
                        principalTable: "roles",
                        principalColumns: new[] { "key", "scope" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "platform_role_assignments",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role_key = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    role_scope = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_platform_role_assignments", x => new { x.user_id, x.role_key });
                    table.CheckConstraint("ck_platform_role_assignments_role_scope", "role_scope = 'Platform'");
                    table.ForeignKey(
                        name: "fk_platform_role_assignments_roles_role_key_role_scope",
                        columns: x => new { x.role_key, x.role_scope },
                        principalTable: "roles",
                        principalColumns: new[] { "key", "scope" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_platform_role_assignments_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "role_permissions",
                columns: table => new
                {
                    role_key = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    permission_key = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    scope = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_role_permissions", x => new { x.role_key, x.permission_key });
                    table.ForeignKey(
                        name: "fk_role_permissions_permissions_permission_key_scope",
                        columns: x => new { x.permission_key, x.scope },
                        principalTable: "permissions",
                        principalColumns: new[] { "key", "scope" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_role_permissions_roles_role_key_scope",
                        columns: x => new { x.role_key, x.scope },
                        principalTable: "roles",
                        principalColumns: new[] { "key", "scope" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "permissions",
                columns: new[] { "key", "description", "scope" },
                values: new object[,]
                {
                    { "billing.manage", "Manage the account's subscription and billing", "Account" },
                    { "platform.operate", "Use the platform operator tools", "Platform" },
                    { "projects.read", "Read the account's projects", "Account" },
                    { "projects.write", "Create, change and delete the account's projects", "Account" },
                    { "reports.generate", "Generate and download the account's reports", "Account" }
                });

            migrationBuilder.InsertData(
                table: "roles",
                columns: new[] { "key", "name", "scope" },
                values: new object[,]
                {
                    { "personal_owner", "Personal account owner", "Account" },
                    { "platform_operator", "Platform operator", "Platform" }
                });

            migrationBuilder.InsertData(
                table: "role_permissions",
                columns: new[] { "permission_key", "role_key", "scope" },
                values: new object[,]
                {
                    { "billing.manage", "personal_owner", "Account" },
                    { "projects.read", "personal_owner", "Account" },
                    { "projects.write", "personal_owner", "Account" },
                    { "reports.generate", "personal_owner", "Account" },
                    { "platform.operate", "platform_operator", "Platform" }
                });

            // Owners registered before roles existed get the role every new registration now writes.
            migrationBuilder.Sql(
                """
                INSERT INTO membership_roles (membership_id, role_key, role_scope, created_at)
                SELECT m.id, 'personal_owner', 'Account', now()
                FROM memberships m
                JOIN accounts a ON a.id = m.account_id AND a.personal_owner_user_id = m.user_id
                WHERE a.type = 'Personal';
                """);

            migrationBuilder.AddCheckConstraint(
                name: "ck_memberships_status",
                table: "memberships",
                sql: "status IN ('Active', 'Suspended')");

            migrationBuilder.CreateIndex(
                name: "ix_membership_roles_role_key_role_scope",
                table: "membership_roles",
                columns: new[] { "role_key", "role_scope" });

            migrationBuilder.CreateIndex(
                name: "ix_platform_role_assignments_role_key_role_scope",
                table: "platform_role_assignments",
                columns: new[] { "role_key", "role_scope" });

            migrationBuilder.CreateIndex(
                name: "ix_role_permissions_permission_key_scope",
                table: "role_permissions",
                columns: new[] { "permission_key", "scope" });

            migrationBuilder.CreateIndex(
                name: "ix_role_permissions_role_key_scope",
                table: "role_permissions",
                columns: new[] { "role_key", "scope" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "membership_roles");

            migrationBuilder.DropTable(
                name: "platform_role_assignments");

            migrationBuilder.DropTable(
                name: "projects");

            migrationBuilder.DropTable(
                name: "role_permissions");

            migrationBuilder.DropTable(
                name: "permissions");

            migrationBuilder.DropTable(
                name: "roles");

            migrationBuilder.DropCheckConstraint(
                name: "ck_memberships_status",
                table: "memberships");

            migrationBuilder.DropColumn(
                name: "status",
                table: "memberships");
        }
    }
}
