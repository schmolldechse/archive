using System;
using Archive.Core.Entities.Authentication.Permissible;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Archive.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddPermissible : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:types.permission_effect", "ALLOW,DENY")
                .Annotation("Npgsql:Enum:types.processing_status", "ABORTED,CANCELLED,COMPLETED,DISCARDED,FAILED,QUEUED,RUNNING")
                .Annotation("Npgsql:Enum:types.progress_step", "FINISHED,LOADING_DYNAMIC_CONTENT,LOADING_PAGE,SAVING_SNAPSHOT,SOURCE_CHECK")
                .Annotation("Npgsql:Enum:types.role", "ADMIN,USER")
                .Annotation("Npgsql:Enum:types.snapshot_quality", "COMPLETE,INCOMPLETE")
                .Annotation("Npgsql:Enum:types.source_type", "HTML,MHTML,URL,WEBARCHIVE")
                .OldAnnotation("Npgsql:Enum:types.processing_status", "ABORTED,CANCELLED,COMPLETED,DISCARDED,FAILED,QUEUED,RUNNING")
                .OldAnnotation("Npgsql:Enum:types.progress_step", "FINISHED,LOADING_DYNAMIC_CONTENT,LOADING_PAGE,SAVING_SNAPSHOT,SOURCE_CHECK")
                .OldAnnotation("Npgsql:Enum:types.snapshot_quality", "COMPLETE,INCOMPLETE")
                .OldAnnotation("Npgsql:Enum:types.source_type", "HTML,MHTML,URL,WEBARCHIVE");

            migrationBuilder.AddColumn<ArchiveRole>(
                name: "role",
                schema: "authentication",
                table: "users",
                type: "types.role",
                nullable: false,
                defaultValue: ArchiveRole.User);

            migrationBuilder.CreateTable(
                name: "user_permission_overrides",
                schema: "authentication",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    permission = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    effect = table.Column<ArchivePermissionEffect>(type: "types.permission_effect", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_permission_overrides", x => new { x.user_id, x.permission });
                    table.ForeignKey(
                        name: "FK_user_permission_overrides_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "authentication",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_permission_overrides",
                schema: "authentication");

            migrationBuilder.DropColumn(
                name: "role",
                schema: "authentication",
                table: "users");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:types.processing_status", "ABORTED,CANCELLED,COMPLETED,DISCARDED,FAILED,QUEUED,RUNNING")
                .Annotation("Npgsql:Enum:types.progress_step", "FINISHED,LOADING_DYNAMIC_CONTENT,LOADING_PAGE,SAVING_SNAPSHOT,SOURCE_CHECK")
                .Annotation("Npgsql:Enum:types.snapshot_quality", "COMPLETE,INCOMPLETE")
                .Annotation("Npgsql:Enum:types.source_type", "HTML,MHTML,URL,WEBARCHIVE")
                .OldAnnotation("Npgsql:Enum:types.permission_effect", "ALLOW,DENY")
                .OldAnnotation("Npgsql:Enum:types.processing_status", "ABORTED,CANCELLED,COMPLETED,DISCARDED,FAILED,QUEUED,RUNNING")
                .OldAnnotation("Npgsql:Enum:types.progress_step", "FINISHED,LOADING_DYNAMIC_CONTENT,LOADING_PAGE,SAVING_SNAPSHOT,SOURCE_CHECK")
                .OldAnnotation("Npgsql:Enum:types.role", "ADMIN,USER")
                .OldAnnotation("Npgsql:Enum:types.snapshot_quality", "COMPLETE,INCOMPLETE")
                .OldAnnotation("Npgsql:Enum:types.source_type", "HTML,MHTML,URL,WEBARCHIVE");
        }
    }
}
