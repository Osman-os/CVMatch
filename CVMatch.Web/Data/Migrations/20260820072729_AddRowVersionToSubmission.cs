using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CVMatch.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRowVersionToSubmission : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CvSubmissions_CandidateProfileId",
                table: "CvSubmissions");

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "CvSubmissions",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.CreateIndex(
                name: "IX_CvSubmissions_CandidateProfileId",
                table: "CvSubmissions",
                column: "CandidateProfileId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CvSubmissions_CandidateProfileId",
                table: "CvSubmissions");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "CvSubmissions");

            migrationBuilder.CreateIndex(
                name: "IX_CvSubmissions_CandidateProfileId",
                table: "CvSubmissions",
                column: "CandidateProfileId",
                unique: true,
                filter: "[CandidateProfileId] IS NOT NULL");
        }
    }
}
