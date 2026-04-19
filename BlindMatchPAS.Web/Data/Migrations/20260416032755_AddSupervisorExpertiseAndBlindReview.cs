using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlindMatchPAS.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSupervisorExpertiseAndBlindReview : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SupervisorExpertise",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SupervisorUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ResearchArea = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupervisorExpertise", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SupervisorExpertise_SupervisorUserId_ResearchArea",
                table: "SupervisorExpertise",
                columns: new[] { "SupervisorUserId", "ResearchArea" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SupervisorExpertise");
        }
    }
}
