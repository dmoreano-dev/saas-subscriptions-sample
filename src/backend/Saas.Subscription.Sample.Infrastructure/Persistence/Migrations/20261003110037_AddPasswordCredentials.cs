using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Saas.Subscription.Sample.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPasswordCredentials : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "users",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Active");

            migrationBuilder.CreateTable(
                name: "password_credentials",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    algorithm = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    encoded_hash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_password_credentials", x => x.user_id);
                    table.CheckConstraint("ck_password_credentials_algorithm", "algorithm IN ('argon2id')");
                    table.CheckConstraint("ck_password_credentials_encoded_hash", "encoded_hash <> ''");
                    table.ForeignKey(
                        name: "fk_password_credentials_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.AddCheckConstraint(
                name: "ck_users_status",
                table: "users",
                sql: "status IN ('Active', 'Disabled')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "password_credentials");

            migrationBuilder.DropCheckConstraint(
                name: "ck_users_status",
                table: "users");

            migrationBuilder.DropColumn(
                name: "status",
                table: "users");
        }
    }
}
