using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlindMatchPAS.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMatchingAndIdentityReveal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MatchAssignments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectProposalId = table.Column<int>(type: "int", nullable: false),
                    StudentUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    SupervisorUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ConfirmedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MatchAssignments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SupervisorInterests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SupervisorUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ProjectProposalId = table.Column<int>(type: "int", nullable: false),
                    ExpressedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupervisorInterests", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MatchAssignments_ProjectProposalId",
                table: "MatchAssignments",
                column: "ProjectProposalId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SupervisorInterests_SupervisorUserId_ProjectProposalId",
                table: "SupervisorInterests",
                columns: new[] { "SupervisorUserId", "ProjectProposalId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MatchAssignments");

            migrationBuilder.DropTable(
                name: "SupervisorInterests");
        }
    }
}
