using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CVMatch.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddJobPostingToSubmissionAndCandidate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "JobPostingId",
                table: "CvSubmissions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "JobPostingId",
                table: "CandidateProfiles",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CvSubmissions_JobPostingId",
                table: "CvSubmissions",
                column: "JobPostingId");

            migrationBuilder.CreateIndex(
                name: "IX_CandidateProfiles_JobPostingId",
                table: "CandidateProfiles",
                column: "JobPostingId");

            migrationBuilder.AddForeignKey(
                name: "FK_CandidateProfiles_JobPostings_JobPostingId",
                table: "CandidateProfiles",
                column: "JobPostingId",
                principalTable: "JobPostings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CvSubmissions_JobPostings_JobPostingId",
                table: "CvSubmissions",
                column: "JobPostingId",
                principalTable: "JobPostings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CandidateProfiles_JobPostings_JobPostingId",
                table: "CandidateProfiles");

            migrationBuilder.DropForeignKey(
                name: "FK_CvSubmissions_JobPostings_JobPostingId",
                table: "CvSubmissions");

            migrationBuilder.DropIndex(
                name: "IX_CvSubmissions_JobPostingId",
                table: "CvSubmissions");

            migrationBuilder.DropIndex(
                name: "IX_CandidateProfiles_JobPostingId",
                table: "CandidateProfiles");

            migrationBuilder.DropColumn(
                name: "JobPostingId",
                table: "CvSubmissions");

            migrationBuilder.DropColumn(
                name: "JobPostingId",
                table: "CandidateProfiles");
        }
    }
}
