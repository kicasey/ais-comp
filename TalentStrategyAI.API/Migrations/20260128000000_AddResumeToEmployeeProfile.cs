using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TalentStrategyAI.API.Migrations
{
    /// <inheritdoc />
    public partial class AddResumeToEmployeeProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ResumeFileName",
                table: "EmployeeProfiles",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResumeFilePath",
                table: "EmployeeProfiles",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ResumeUploadedAt",
                table: "EmployeeProfiles",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "ResumeFileName", table: "EmployeeProfiles");
            migrationBuilder.DropColumn(name: "ResumeFilePath", table: "EmployeeProfiles");
            migrationBuilder.DropColumn(name: "ResumeUploadedAt", table: "EmployeeProfiles");
        }
    }
}
