using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CVMatch.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class UniqueSubmissionProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CvSubmissions_CandidateProfileId",
                table: "CvSubmissions");

            migrationBuilder.CreateIndex(
                name: "IX_CvSubmissions_CandidateProfileId",
                table: "CvSubmissions",
                column: "CandidateProfileId",
                unique: true,
                filter: "[CandidateProfileId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CvSubmissions_CandidateProfileId",
                table: "CvSubmissions");

            migrationBuilder.CreateIndex(
                name: "IX_CvSubmissions_CandidateProfileId",
                table: "CvSubmissions",
                column: "CandidateProfileId");
        }
    }
}
