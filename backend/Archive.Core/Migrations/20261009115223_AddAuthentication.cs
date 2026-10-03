using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Archive.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddAuthentication : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "authentication");

            migrationBuilder.CreateTable(
                name: "auth_providers",
                schema: "authentication",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider_key = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    display_name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_auth_providers", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                schema: "authentication",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    display_name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    handle = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    profile_image = table.Column<byte[]>(type: "bytea", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "user_auth_identities",
                schema: "authentication",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    auth_provider_id = table.Column<Guid>(type: "uuid", nullable: false),
                    external_subject = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_login_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_auth_identities", x => x.id);
                    table.ForeignKey(
                        name: "FK_user_auth_identities_auth_providers_auth_provider_id",
                        column: x => x.auth_provider_id,
                        principalSchema: "authentication",
                        principalTable: "auth_providers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_user_auth_identities_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "authentication",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_auth_providers_provider_key",
                schema: "authentication",
                table: "auth_providers",
                column: "provider_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_auth_identities_auth_provider_id_external_subject",
                schema: "authentication",
                table: "user_auth_identities",
                columns: new[] { "auth_provider_id", "external_subject" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_auth_identities_user_id_auth_provider_id",
                schema: "authentication",
                table: "user_auth_identities",
                columns: new[] { "user_id", "auth_provider_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_handle",
                schema: "authentication",
                table: "users",
                column: "handle",
                unique: true,
                filter: "handle <> ''");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_auth_identities",
                schema: "authentication");

            migrationBuilder.DropTable(
                name: "auth_providers",
                schema: "authentication");

            migrationBuilder.DropTable(
                name: "users",
                schema: "authentication");
        }
    }
}
