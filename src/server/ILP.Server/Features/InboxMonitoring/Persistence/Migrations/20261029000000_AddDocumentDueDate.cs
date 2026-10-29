using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ILP.Server.Features.InboxMonitoring.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentDueDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DueDate",
                table: "Documents",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DueDate",
                table: "Documents");
        }
    }
}
