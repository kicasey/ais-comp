using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TalentStrategyAI.API.Migrations
{
    public partial class AddResumeToEmployeeProfile : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ResumeFileName",
                table: "EmployeeProfiles",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResumeFilePath",
                table: "EmployeeProfiles",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ResumeUploadedAt",
                table: "EmployeeProfiles",
                type: "TEXT",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "ResumeFileName", table: "EmployeeProfiles");
            migrationBuilder.DropColumn(name: "ResumeFilePath", table: "EmployeeProfiles");
            migrationBuilder.DropColumn(name: "ResumeUploadedAt", table: "EmployeeProfiles");
        }
    }
}
