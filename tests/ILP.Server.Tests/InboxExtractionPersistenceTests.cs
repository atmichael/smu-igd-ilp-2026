using ILP.Server.Features.InboxMonitoring.Persistence;
using ILP.Server.Features.InboxMonitoring.Persistence.Entity;
using ILP.Server.Features.InboxMonitoring.Persistence.Repository;
using ILP.Shared.InboxMonitoring.Dto;
using ILP.Shared.TransactionDocument.Dto;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ILP.Server.Tests;

public sealed class InboxExtractionPersistenceTests
{
    [Fact]
    public async Task SaveAsync_AppliesMigrationAndPersistsNormalizedRecordsAndSourceLinks()
    {
        string databasePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.db");
        var options = new DbContextOptionsBuilder<InboxExtractionDbContext>()
            .UseSqlite($"Data Source={databasePath}")
            .Options;
        var factory = new TestContextFactory(options);
        var persistence = new SqliteInboxExtractionRepo(factory);
        var attachment = new InboxMessageAttachmentDto(
            "invoice.pdf",
            "drive-123",
            "https://drive.google.com/file/d/drive-123/view");
        var message = new InboxMessageDto
        {
            UniqueId = 21,
            Subject = "Invoice",
            From = "supplier@example.com",
            Body = "Invoice attached.",
            GoogleDriveAttachments = [attachment]
        };
        var document = new DocumentDto(
            "INV-21",
            "Invoice",
            new DateTime(2026, 10, 24),
            12.50m,
            [new DocumentItemDto(2, 6.25m, description: "Parts", amount: 12.50m)],
            company: new CompanyDto("Supplier", "UEN-21", "1 Road", email: "ap@supplier.example"),
            confidence: 0.91m);

        try
        {
            await using (var context = await factory.CreateDbContextAsync())
            {
                await context.Database.MigrateAsync();
            }

            var secondDocument = new DocumentDto(
                "INV-22",
                "Invoice",
                new DateTime(2026, 10, 24),
                20m,
                company: new CompanyDto("Supplier", "UEN-21"));
            await persistence.SaveAsync(
            [
                new DocumentInboxMessageMappingDto(document, message, attachment),
                new DocumentInboxMessageMappingDto(secondDocument, message)
            ]);

            await using var verifyContext = await factory.CreateDbContextAsync();
            var mappings = await verifyContext.DocumentInboxMessages
                .Include(item => item.Document)
                    .ThenInclude(item => item.Company)
                .Include(item => item.Document)
                    .ThenInclude(item => item.DocumentItems)
                .Include(item => item.Message)
                    .ThenInclude(item => item.Attachments)
                .Include(item => item.SourceAttachment)
                .OrderBy(item => item.Document.RefNumber)
                .ToListAsync();
            var mapping = mappings[0];

            Assert.Equal(2, mappings.Count);
            Assert.Equal("INV-21", mapping.Document.RefNumber);
            Assert.Equal(0.91m, mapping.Document.Confidence);
            Assert.Equal("UEN-21", mapping.Document.Company.Uen);
            Assert.Equal("ap@supplier.example", mapping.Document.Company.Email);
            Assert.Equal("1 Road", mapping.Document.Company.Address);
            Assert.Equal("Parts", Assert.Single(mapping.Document.DocumentItems).Description);
            Assert.Equal(21u, mapping.Message.UniqueId);
            Assert.Equal(attachment.WebViewLink, mapping.SourceAttachment?.WebViewLink);
            Assert.Equal(mapping.Document.CompanyId, mappings[1].Document.CompanyId);
            Assert.All(
                new AuditedEntity[]
                {
                    mapping.Document,
                    mapping.Document.Company,
                    Assert.Single(mapping.Document.DocumentItems),
                    mapping.Message,
                    Assert.Single(mapping.Message.Attachments),
                    mapping
                },
                entity =>
                {
                    Assert.Equal(1, entity.Version);
                    Assert.True(entity.ActiveStatus);
                    Assert.NotEqual(default, entity.CreatedTime);
                    Assert.NotEqual(default, entity.ModifiedTime);
                });
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            File.Delete(databasePath);
        }
    }

    private sealed class TestContextFactory(DbContextOptions<InboxExtractionDbContext> options)
        : IDbContextFactory<InboxExtractionDbContext>
    {
        public InboxExtractionDbContext CreateDbContext() => new(options);

        public Task<InboxExtractionDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(CreateDbContext());
    }
}
