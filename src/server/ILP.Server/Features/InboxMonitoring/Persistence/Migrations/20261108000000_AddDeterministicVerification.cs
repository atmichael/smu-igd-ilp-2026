using ILP.Server.Features.InboxMonitoring.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ILP.Server.Features.InboxMonitoring.Persistence.Migrations
{
    [DbContext(typeof(InboxExtractionDbContext))]
    [Migration("20261108000000_AddDeterministicVerification")]
    public partial class AddDeterministicVerification : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VerificationRuns",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false).Annotation("Sqlite:Autoincrement", true),
                    VerificationRunId = table.Column<string>(type: "TEXT", nullable: false),
                    DocumentId = table.Column<string>(type: "TEXT", nullable: false),
                    RunJson = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_VerificationRuns", x => x.Id));

            migrationBuilder.CreateTable(
                name: "VerificationReviewResolutions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false).Annotation("Sqlite:Autoincrement", true),
                    ReviewItemId = table.Column<string>(type: "TEXT", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    ResolutionReason = table.Column<string>(type: "TEXT", nullable: false),
                    ResolvedAt = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_VerificationReviewResolutions", x => x.Id));

            migrationBuilder.CreateIndex(name: "IX_VerificationRuns_VerificationRunId", table: "VerificationRuns", column: "VerificationRunId", unique: true);
            migrationBuilder.CreateIndex(name: "IX_VerificationReviewResolutions_ReviewItemId", table: "VerificationReviewResolutions", column: "ReviewItemId", unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "VerificationReviewResolutions");
            migrationBuilder.DropTable(name: "VerificationRuns");
        }
    }
}
