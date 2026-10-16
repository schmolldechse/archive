using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Archive.Core.Migrations
{
    /// <inheritdoc />
    public partial class RemoveProjects : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_snapshots_projects_project_id",
                schema: "archive",
                table: "snapshots");

            migrationBuilder.DropIndex(
                name: "IX_snapshots_project_id",
                schema: "archive",
                table: "snapshots");

            migrationBuilder.DropColumn(
                name: "project_id",
                schema: "archive",
                table: "snapshots");

            migrationBuilder.DropTable(
                name: "projects",
                schema: "archive");

            migrationBuilder.Sql("DROP EXTENSION IF EXISTS pgcrypto RESTRICT;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            throw new NotSupportedException("Project removal is permanent and cannot be downgraded.");
        }
    }
}
