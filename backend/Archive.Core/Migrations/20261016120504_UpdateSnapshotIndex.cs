using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Archive.Core.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSnapshotIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_snapshots_origin_url_created_at",
                schema: "archive",
                table: "snapshots");

            migrationBuilder.CreateIndex(
                name: "IX_snapshots_origin_url",
                schema: "archive",
                table: "snapshots",
                column: "origin_url")
                .Annotation("Npgsql:IndexMethod", "hash");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_snapshots_origin_url",
                schema: "archive",
                table: "snapshots");

            migrationBuilder.CreateIndex(
                name: "IX_snapshots_origin_url_created_at",
                schema: "archive",
                table: "snapshots",
                columns: new[] { "origin_url", "created_at" });
        }
    }
}
