using ILP.Server.Features.InboxMonitoring.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ILP.Server.Features.InboxMonitoring.Persistence.Migrations;

[DbContext(typeof(InboxExtractionDbContext))]
[Migration("20261104193000_AddDocumentAPJournal")]
public sealed class AddDocumentAPJournal : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "DocumentAPJournals",
            columns: table => new
            {
                Id = table.Column<long>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                DocumentId = table.Column<long>(type: "INTEGER", nullable: false),
                Confidence = table.Column<decimal>(type: "TEXT", precision: 5, scale: 4, nullable: true),
                GstCode = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                CoaSuggestionsJson = table.Column<string>(type: "TEXT", nullable: false),
                VerificationNotes = table.Column<string>(type: "TEXT", nullable: false),
                Version = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                ActiveStatus = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                CreatedTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                ModifiedTime = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DocumentAPJournals", journal => journal.Id);
                table.CheckConstraint("CK_DocumentAPJournals_ActiveStatus", "\"ActiveStatus\" IN (0, 1)");
                table.ForeignKey(
                    name: "FK_DocumentAPJournals_Documents_DocumentId",
                    column: journal => journal.DocumentId,
                    principalTable: "Documents",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_DocumentAPJournals_DocumentId",
            table: "DocumentAPJournals",
            column: "DocumentId",
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropTable(name: "DocumentAPJournals");
}
