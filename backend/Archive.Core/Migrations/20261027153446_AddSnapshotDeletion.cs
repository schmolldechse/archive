using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Archive.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddSnapshotDeletion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "snapshot_deletions",
                schema: "processing",
                columns: table => new
                {
                    snapshot_id = table.Column<Guid>(type: "uuid", nullable: false),
                    content_prefix = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    uploaded_object_key = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    cleanup_after = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    next_attempt_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    attempt_count = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_snapshot_deletions", x => x.snapshot_id);
                    table.CheckConstraint("CK_snapshot_deletions_attempt_count", "attempt_count >= 0");
                });

            migrationBuilder.CreateIndex(
                name: "IX_snapshot_deletions_next_attempt_at_snapshot_id",
                schema: "processing",
                table: "snapshot_deletions",
                columns: new[] { "next_attempt_at", "snapshot_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "snapshot_deletions",
                schema: "processing");
        }
    }
}
