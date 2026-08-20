using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CVMatch.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddProcessingStartedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ProcessingStartedAt",
                table: "CvSubmissions",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProcessingStartedAt",
                table: "CvSubmissions");
        }
    }
}
