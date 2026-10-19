using System;
using System.Text.Json;
using Archive.Core.Entities;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Archive.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddSnapshotHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_snapshots_created_by_user_id",
                schema: "archive",
                table: "snapshots");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:types.permission_effect", "ALLOW,DENY")
                .Annotation("Npgsql:Enum:types.processing_status", "ABORTED,CANCELLED,COMPLETED,DISCARDED,FAILED,QUEUED,RUNNING")
                .Annotation("Npgsql:Enum:types.progress_step", "FINISHED,LOADING_DYNAMIC_CONTENT,LOADING_PAGE,SAVING_SNAPSHOT,SOURCE_CHECK")
                .Annotation("Npgsql:Enum:types.role", "ADMIN,USER")
                .Annotation("Npgsql:Enum:types.snapshot_moderation_status", "APPROVED,PENDING,REJECTED,RESUBMITTED")
                .Annotation("Npgsql:Enum:types.snapshot_quality", "COMPLETE,INCOMPLETE")
                .Annotation("Npgsql:Enum:types.source_type", "HTML,MHTML,URL,WEBARCHIVE")
                .OldAnnotation("Npgsql:Enum:types.permission_effect", "ALLOW,DENY")
                .OldAnnotation("Npgsql:Enum:types.processing_status", "ABORTED,CANCELLED,COMPLETED,DISCARDED,FAILED,QUEUED,RUNNING")
                .OldAnnotation("Npgsql:Enum:types.progress_step", "FINISHED,LOADING_DYNAMIC_CONTENT,LOADING_PAGE,SAVING_SNAPSHOT,SOURCE_CHECK")
                .OldAnnotation("Npgsql:Enum:types.role", "ADMIN,USER")
                .OldAnnotation("Npgsql:Enum:types.snapshot_quality", "COMPLETE,INCOMPLETE")
                .OldAnnotation("Npgsql:Enum:types.source_type", "HTML,MHTML,URL,WEBARCHIVE");

            migrationBuilder.AddColumn<SnapshotModerationStatus>(
                name: "moderation_status",
                schema: "archive",
                table: "snapshots",
                type: "types.snapshot_moderation_status",
                nullable: false,
                defaultValue: SnapshotModerationStatus.Pending);

            migrationBuilder.CreateTable(
                name: "snapshot_history",
                schema: "archive",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    snapshot_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sequence = table.Column<long>(type: "bigint", nullable: false),
                    actor_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    content = table.Column<JsonElement>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_snapshot_history", x => x.id);
                    table.CheckConstraint("CK_snapshot_history_sequence", "sequence > 0");
                    table.ForeignKey(
                        name: "FK_snapshot_history_snapshots_snapshot_id",
                        column: x => x.snapshot_id,
                        principalSchema: "archive",
                        principalTable: "snapshots",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_snapshot_history_users_actor_user_id",
                        column: x => x.actor_user_id,
                        principalSchema: "authentication",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_snapshots_created_by_user_id_created_at_id",
                schema: "archive",
                table: "snapshots",
                columns: new[] { "created_by_user_id", "created_at", "id" });

            migrationBuilder.CreateIndex(
                name: "IX_snapshot_history_actor_user_id",
                schema: "archive",
                table: "snapshot_history",
                column: "actor_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_snapshot_history_snapshot_id_sequence",
                schema: "archive",
                table: "snapshot_history",
                columns: new[] { "snapshot_id", "sequence" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "snapshot_history",
                schema: "archive");

            migrationBuilder.DropIndex(
                name: "IX_snapshots_created_by_user_id_created_at_id",
                schema: "archive",
                table: "snapshots");

            migrationBuilder.DropColumn(
                name: "moderation_status",
                schema: "archive",
                table: "snapshots");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:types.permission_effect", "ALLOW,DENY")
                .Annotation("Npgsql:Enum:types.processing_status", "ABORTED,CANCELLED,COMPLETED,DISCARDED,FAILED,QUEUED,RUNNING")
                .Annotation("Npgsql:Enum:types.progress_step", "FINISHED,LOADING_DYNAMIC_CONTENT,LOADING_PAGE,SAVING_SNAPSHOT,SOURCE_CHECK")
                .Annotation("Npgsql:Enum:types.role", "ADMIN,USER")
                .Annotation("Npgsql:Enum:types.snapshot_quality", "COMPLETE,INCOMPLETE")
                .Annotation("Npgsql:Enum:types.source_type", "HTML,MHTML,URL,WEBARCHIVE")
                .OldAnnotation("Npgsql:Enum:types.permission_effect", "ALLOW,DENY")
                .OldAnnotation("Npgsql:Enum:types.processing_status", "ABORTED,CANCELLED,COMPLETED,DISCARDED,FAILED,QUEUED,RUNNING")
                .OldAnnotation("Npgsql:Enum:types.progress_step", "FINISHED,LOADING_DYNAMIC_CONTENT,LOADING_PAGE,SAVING_SNAPSHOT,SOURCE_CHECK")
                .OldAnnotation("Npgsql:Enum:types.role", "ADMIN,USER")
                .OldAnnotation("Npgsql:Enum:types.snapshot_moderation_status", "APPROVED,PENDING,REJECTED,RESUBMITTED")
                .OldAnnotation("Npgsql:Enum:types.snapshot_quality", "COMPLETE,INCOMPLETE")
                .OldAnnotation("Npgsql:Enum:types.source_type", "HTML,MHTML,URL,WEBARCHIVE");

            migrationBuilder.CreateIndex(
                name: "IX_snapshots_created_by_user_id",
                schema: "archive",
                table: "snapshots",
                column: "created_by_user_id");
        }
    }
}
