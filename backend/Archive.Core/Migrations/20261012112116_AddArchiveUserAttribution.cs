using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Archive.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddArchiveUserAttribution : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "created_by_user_id",
                schema: "archive",
                table: "snapshots",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "created_by_user_id",
                schema: "processing",
                table: "archive_jobs",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_snapshots_created_by_user_id",
                schema: "archive",
                table: "snapshots",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_archive_jobs_created_by_user_id",
                schema: "processing",
                table: "archive_jobs",
                column: "created_by_user_id");

            migrationBuilder.AddForeignKey(
                name: "FK_archive_jobs_users_created_by_user_id",
                schema: "processing",
                table: "archive_jobs",
                column: "created_by_user_id",
                principalSchema: "authentication",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_snapshots_users_created_by_user_id",
                schema: "archive",
                table: "snapshots",
                column: "created_by_user_id",
                principalSchema: "authentication",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_archive_jobs_users_created_by_user_id",
                schema: "processing",
                table: "archive_jobs");

            migrationBuilder.DropForeignKey(
                name: "FK_snapshots_users_created_by_user_id",
                schema: "archive",
                table: "snapshots");

            migrationBuilder.DropIndex(
                name: "IX_snapshots_created_by_user_id",
                schema: "archive",
                table: "snapshots");

            migrationBuilder.DropIndex(
                name: "IX_archive_jobs_created_by_user_id",
                schema: "processing",
                table: "archive_jobs");

            migrationBuilder.DropColumn(
                name: "created_by_user_id",
                schema: "archive",
                table: "snapshots");

            migrationBuilder.DropColumn(
                name: "created_by_user_id",
                schema: "processing",
                table: "archive_jobs");
        }
    }
}
