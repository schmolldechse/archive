using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Archive.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddUploadFormats : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:types.job_status", "ABORTED,CANCELLED,COMPLETED,DISCARDED,FAILED,QUEUED,RUNNING")
                .Annotation("Npgsql:Enum:types.progress_step", "FINISHED,LOADING_DYNAMIC_CONTENT,LOADING_PAGE,SAVING_SNAPSHOT,SOURCE_CHECK")
                .Annotation("Npgsql:Enum:types.snapshot_quality", "COMPLETE,INCOMPLETE")
                .Annotation("Npgsql:Enum:types.source_type", "HTML,MHTML,URL,WEBARCHIVE")
                .OldAnnotation("Npgsql:Enum:types.job_status", "ABORTED,CANCELLED,COMPLETED,DISCARDED,FAILED,QUEUED,RUNNING")
                .OldAnnotation("Npgsql:Enum:types.progress_step", "FINISHED,LOADING_DYNAMIC_CONTENT,LOADING_PAGE,SAVING_SNAPSHOT,SOURCE_CHECK")
                .OldAnnotation("Npgsql:Enum:types.snapshot_quality", "COMPLETE,INCOMPLETE")
                .OldAnnotation("Npgsql:Enum:types.source_type", "HTML,URL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:types.job_status", "ABORTED,CANCELLED,COMPLETED,DISCARDED,FAILED,QUEUED,RUNNING")
                .Annotation("Npgsql:Enum:types.progress_step", "FINISHED,LOADING_DYNAMIC_CONTENT,LOADING_PAGE,SAVING_SNAPSHOT,SOURCE_CHECK")
                .Annotation("Npgsql:Enum:types.snapshot_quality", "COMPLETE,INCOMPLETE")
                .Annotation("Npgsql:Enum:types.source_type", "HTML,URL")
                .OldAnnotation("Npgsql:Enum:types.job_status", "ABORTED,CANCELLED,COMPLETED,DISCARDED,FAILED,QUEUED,RUNNING")
                .OldAnnotation("Npgsql:Enum:types.progress_step", "FINISHED,LOADING_DYNAMIC_CONTENT,LOADING_PAGE,SAVING_SNAPSHOT,SOURCE_CHECK")
                .OldAnnotation("Npgsql:Enum:types.snapshot_quality", "COMPLETE,INCOMPLETE")
                .OldAnnotation("Npgsql:Enum:types.source_type", "HTML,MHTML,URL,WEBARCHIVE");
        }
    }
}
