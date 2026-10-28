using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ILP.Server.Features.InboxMonitoring.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentCompositeUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Documents_CompanyId",
                table: "Documents");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_CompanyId_TypeCode_RefNumber",
                table: "Documents",
                columns: new[] { "CompanyId", "TypeCode", "RefNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Documents_CompanyId_TypeCode_RefNumber",
                table: "Documents");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_CompanyId",
                table: "Documents",
                column: "CompanyId");
        }
    }
}
