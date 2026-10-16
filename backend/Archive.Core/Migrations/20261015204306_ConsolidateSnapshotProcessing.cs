using System;
using Archive.Core.Entities;
using Archive.Core.Entities.Processing;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Archive.Core.Migrations
{
    /// <inheritdoc />
    public partial class ConsolidateSnapshotProcessing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "archive_job_tags",
                schema: "processing");

            migrationBuilder.DropTable(
                name: "archive_jobs",
                schema: "processing");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:types.processing_status", "ABORTED,CANCELLED,COMPLETED,DISCARDED,FAILED,QUEUED,RUNNING")
                .Annotation("Npgsql:Enum:types.progress_step", "FINISHED,LOADING_DYNAMIC_CONTENT,LOADING_PAGE,SAVING_SNAPSHOT,SOURCE_CHECK")
                .Annotation("Npgsql:Enum:types.snapshot_quality", "COMPLETE,INCOMPLETE")
                .Annotation("Npgsql:Enum:types.source_type", "HTML,MHTML,URL,WEBARCHIVE")
                .OldAnnotation("Npgsql:Enum:types.job_status", "ABORTED,CANCELLED,COMPLETED,DISCARDED,FAILED,QUEUED,RUNNING")
                .OldAnnotation("Npgsql:Enum:types.progress_step", "FINISHED,LOADING_DYNAMIC_CONTENT,LOADING_PAGE,SAVING_SNAPSHOT,SOURCE_CHECK")
                .OldAnnotation("Npgsql:Enum:types.snapshot_quality", "COMPLETE,INCOMPLETE")
                .OldAnnotation("Npgsql:Enum:types.source_type", "HTML,MHTML,URL,WEBARCHIVE");

            migrationBuilder.AlterColumn<long>(
                name: "storage_bytes",
                schema: "archive",
                table: "snapshots",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AlterColumn<int>(
                name: "resource_count",
                schema: "archive",
                table: "snapshots",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<SnapshotQuality>(
                name: "quality",
                schema: "archive",
                table: "snapshots",
                type: "types.snapshot_quality",
                nullable: true,
                oldClrType: typeof(SnapshotQuality),
                oldType: "types.snapshot_quality");

            migrationBuilder.AlterColumn<long>(
                name: "duration_ms",
                schema: "archive",
                table: "snapshots",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AlterColumn<string>(
                name: "content_prefix",
                schema: "archive",
                table: "snapshots",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(512)",
                oldMaxLength: 512);

            migrationBuilder.CreateTable(
                name: "snapshot_processing",
                schema: "processing",
                columns: table => new
                {
                    snapshot_id = table.Column<Guid>(type: "uuid", nullable: false),
                    queue_sequence = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    status = table.Column<ProcessingStatus>(type: "types.processing_status", nullable: false),
                    progress_step = table.Column<ProgressStep>(type: "types.progress_step", nullable: false),
                    owner_token = table.Column<Guid>(type: "uuid", nullable: true),
                    cancel_requested = table.Column<bool>(type: "boolean", nullable: false),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    uploaded_object_key = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_snapshot_processing", x => x.snapshot_id);
                    table.CheckConstraint("CK_snapshot_processing_cancel", "NOT cancel_requested OR status = 'RUNNING'");
                    table.CheckConstraint("CK_snapshot_processing_owner", "(status = 'RUNNING' AND owner_token IS NOT NULL AND started_at IS NOT NULL)\nOR (status <> 'RUNNING' AND owner_token IS NULL)");
                    table.CheckConstraint("CK_snapshot_processing_step", "(status <> 'QUEUED' OR progress_step = 'SOURCE_CHECK')\nAND ((status = 'COMPLETED' AND progress_step = 'FINISHED')\n    OR (status <> 'COMPLETED' AND progress_step <> 'FINISHED'))");
                    table.CheckConstraint("CK_snapshot_processing_terminal", "(status IN ('QUEUED', 'RUNNING') AND completed_at IS NULL AND expires_at IS NULL)\nOR (status NOT IN ('QUEUED', 'RUNNING') AND completed_at IS NOT NULL\n    AND expires_at IS NOT NULL AND expires_at >= completed_at)");
                    table.ForeignKey(
                        name: "FK_snapshot_processing_snapshots_snapshot_id",
                        column: x => x.snapshot_id,
                        principalSchema: "archive",
                        principalTable: "snapshots",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_snapshot_processing_expires_at_snapshot_id",
                schema: "processing",
                table: "snapshot_processing",
                columns: new[] { "expires_at", "snapshot_id" },
                filter: "expires_at IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_snapshot_processing_queue_sequence",
                schema: "processing",
                table: "snapshot_processing",
                column: "queue_sequence",
                filter: "status = 'QUEUED'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Dropped legacy jobs cannot be reconstructed, and unfinished snapshots have no old-schema equivalent.
            throw new NotSupportedException("Restore the pre-upgrade backup with its matching application version to roll back.");
        }
    }
}