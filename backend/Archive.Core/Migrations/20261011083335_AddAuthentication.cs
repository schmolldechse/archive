using System;
using Archive.Core.Entities;
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
                name: "processing");

            migrationBuilder.EnsureSchema(
                name: "authentication");

            migrationBuilder.EnsureSchema(
                name: "archive");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:types.job_status", "ABORTED,CANCELLED,COMPLETED,DISCARDED,FAILED,QUEUED,RUNNING")
                .Annotation("Npgsql:Enum:types.progress_step", "FINISHED,LOADING_DYNAMIC_CONTENT,LOADING_PAGE,SAVING_SNAPSHOT,SOURCE_CHECK")
                .Annotation("Npgsql:Enum:types.snapshot_quality", "COMPLETE,INCOMPLETE")
                .Annotation("Npgsql:Enum:types.source_type", "HTML,MHTML,URL,WEBARCHIVE");

            migrationBuilder.CreateTable(
                name: "archive_jobs",
                schema: "processing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_type = table.Column<SourceType>(type: "types.source_type", nullable: false),
                    origin_url = table.Column<string>(type: "character varying(4096)", maxLength: 4096, nullable: true),
                    uploaded_object_key = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    title = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    description = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    status = table.Column<JobStatus>(type: "types.job_status", nullable: false),
                    progress_step = table.Column<ProgressStep>(type: "types.progress_step", nullable: false),
                    progress_percent = table.Column<int>(type: "integer", nullable: false),
                    error_message = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_archive_jobs", x => x.id);
                });

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
                name: "projects",
                schema: "archive",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_key = table.Column<string>(type: "character varying(4500)", maxLength: 4500, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_projects", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tags",
                schema: "archive",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    value = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tags", x => x.id);
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
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    registration_completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "archive_job_tags",
                schema: "processing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    job_id = table.Column<Guid>(type: "uuid", nullable: false),
                    value = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_archive_job_tags", x => x.id);
                    table.ForeignKey(
                        name: "FK_archive_job_tags_archive_jobs_job_id",
                        column: x => x.job_id,
                        principalSchema: "processing",
                        principalTable: "archive_jobs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "snapshots",
                schema: "archive",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_type = table.Column<SourceType>(type: "types.source_type", nullable: false),
                    origin_url = table.Column<string>(type: "character varying(4096)", maxLength: 4096, nullable: true),
                    title = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    description = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    quality = table.Column<SnapshotQuality>(type: "types.snapshot_quality", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    capture_started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    capture_completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    duration_ms = table.Column<long>(type: "bigint", nullable: false),
                    storage_bytes = table.Column<long>(type: "bigint", nullable: false),
                    resource_count = table.Column<int>(type: "integer", nullable: false),
                    content_prefix = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_snapshots", x => x.id);
                    table.ForeignKey(
                        name: "FK_snapshots_projects_project_id",
                        column: x => x.project_id,
                        principalSchema: "archive",
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
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

            migrationBuilder.CreateTable(
                name: "snapshot_tags",
                schema: "archive",
                columns: table => new
                {
                    snapshot_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tag_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_snapshot_tags", x => new { x.snapshot_id, x.tag_id });
                    table.ForeignKey(
                        name: "FK_snapshot_tags_snapshots_snapshot_id",
                        column: x => x.snapshot_id,
                        principalSchema: "archive",
                        principalTable: "snapshots",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_snapshot_tags_tags_tag_id",
                        column: x => x.tag_id,
                        principalSchema: "archive",
                        principalTable: "tags",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_archive_job_tags_job_id_value",
                schema: "processing",
                table: "archive_job_tags",
                columns: new[] { "job_id", "value" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_archive_jobs_status_created_at",
                schema: "processing",
                table: "archive_jobs",
                columns: new[] { "status", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_auth_providers_provider_key",
                schema: "authentication",
                table: "auth_providers",
                column: "provider_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_projects_project_key",
                schema: "archive",
                table: "projects",
                column: "project_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_snapshot_tags_tag_id",
                schema: "archive",
                table: "snapshot_tags",
                column: "tag_id");

            migrationBuilder.CreateIndex(
                name: "IX_snapshots_created_at",
                schema: "archive",
                table: "snapshots",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "IX_snapshots_origin_url_created_at",
                schema: "archive",
                table: "snapshots",
                columns: new[] { "origin_url", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_snapshots_project_id",
                schema: "archive",
                table: "snapshots",
                column: "project_id");

            migrationBuilder.CreateIndex(
                name: "IX_tags_value",
                schema: "archive",
                table: "tags",
                column: "value",
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
                name: "archive_job_tags",
                schema: "processing");

            migrationBuilder.DropTable(
                name: "snapshot_tags",
                schema: "archive");

            migrationBuilder.DropTable(
                name: "user_auth_identities",
                schema: "authentication");

            migrationBuilder.DropTable(
                name: "archive_jobs",
                schema: "processing");

            migrationBuilder.DropTable(
                name: "snapshots",
                schema: "archive");

            migrationBuilder.DropTable(
                name: "tags",
                schema: "archive");

            migrationBuilder.DropTable(
                name: "auth_providers",
                schema: "authentication");

            migrationBuilder.DropTable(
                name: "users",
                schema: "authentication");

            migrationBuilder.DropTable(
                name: "projects",
                schema: "archive");
        }
    }
}
