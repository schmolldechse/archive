using System;
using Archive.Core.Entities;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Archive.Core.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "processing");

            migrationBuilder.EnsureSchema(
                name: "archive");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:types.job_status", "ABORTED,CANCELLED,COMPLETED,DISCARDED,FAILED,QUEUED,RUNNING")
                .Annotation("Npgsql:Enum:types.progress_step", "FINISHED,LOADING_DYNAMIC_CONTENT,LOADING_PAGE,SAVING_SNAPSHOT,SOURCE_CHECK")
                .Annotation("Npgsql:Enum:types.snapshot_quality", "COMPLETE,INCOMPLETE")
                .Annotation("Npgsql:Enum:types.source_type", "HTML,URL");

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
                name: "archive_jobs",
                schema: "processing");

            migrationBuilder.DropTable(
                name: "snapshots",
                schema: "archive");

            migrationBuilder.DropTable(
                name: "tags",
                schema: "archive");

            migrationBuilder.DropTable(
                name: "projects",
                schema: "archive");
        }
    }
}
