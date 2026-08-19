using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CVMatch.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveIsAiExtracted : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsAiExtracted",
                table: "CandidateSkills");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsAiExtracted",
                table: "CandidateSkills",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
