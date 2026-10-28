using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Archive.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddPrivateSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_private",
                schema: "archive",
                table: "snapshots",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_private",
                schema: "archive",
                table: "snapshots");
        }
    }
}
