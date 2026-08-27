using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GarageOS.Infrastructure.Data.Migrations.App
{
    /// <inheritdoc />
    public partial class AddUserLockoutColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "failed_login_attempts",
                table: "users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "lockout_end_at",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "password_reset_tokens",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token_hash = table.Column<string>(type: "text", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    used_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    requested_by_ip = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_password_reset_tokens", x => x.id);
                    table.ForeignKey(
                        name: "fk_password_reset_tokens_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "password_reset_tokens_token_hash_idx",
                table: "password_reset_tokens",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "password_reset_tokens_user_idx",
                table: "password_reset_tokens",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "password_reset_tokens");

            migrationBuilder.DropColumn(
                name: "failed_login_attempts",
                table: "users");

            migrationBuilder.DropColumn(
                name: "lockout_end_at",
                table: "users");
        }
    }
}
