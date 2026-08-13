using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CVMatch.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPreferredEmploymentType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PreferredEmploymentType",
                table: "CandidateProfiles",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_CandidateProfiles_PreferredEmploymentType",
                table: "CandidateProfiles",
                column: "PreferredEmploymentType");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CandidateProfiles_PreferredEmploymentType",
                table: "CandidateProfiles");

            migrationBuilder.DropColumn(
                name: "PreferredEmploymentType",
                table: "CandidateProfiles");
        }
    }
}
