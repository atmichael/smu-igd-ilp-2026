using ILP.Server.Features.AccountsPayable.Service;
using ILP.Server.Features.AccountsPayable.Persistence.Entity;
using ILP.Server.Features.InboxMonitoring.Persistence;
using ILP.Server.Features.InboxMonitoring.Persistence.Entity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using ILP.Shared.AccountsPayable.Dto;
using Xunit;

namespace ILP.Server.Tests.AccountsPayable;

public sealed class AccountsPayableReferenceDataTests
{
    [Fact]
    public async Task GetReferenceDataAsync_AppliesMigrationAndPersistsSkillReferenceValues()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.db");
        var options = new DbContextOptionsBuilder<InboxExtractionDbContext>()
            .UseSqlite($"Data Source={databasePath}")
            .Options;

        try
        {
            var factory = new TestContextFactory(options);
            await using (var context = await factory.CreateDbContextAsync())
            {
                await context.Database.MigrateAsync();
            }

            var referenceData = await new AccountsPayableService(factory)
                .GetReferenceDataAsync(TestContext.Current.CancellationToken);

            Assert.Equal(38, referenceData.Accounts.Count);
            Assert.Equal(8, referenceData.GstCodes.Count);
            Assert.True(referenceData.Accounts.Single(account => account.Code == "6000").Enabled);
            Assert.False(referenceData.Accounts.Single(account => account.Code == "5010").Enabled);
            Assert.Equal("SG_STD_9", referenceData.GstCodes.Single(code => code.Code == "SG_STD_9").Code);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            File.Delete(databasePath);
        }
    }

    [Fact]
    public async Task GetCodingAsync_ReturnsThePersistedDocumentApJournal()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.db");
        var options = new DbContextOptionsBuilder<InboxExtractionDbContext>()
            .UseSqlite($"Data Source={databasePath}")
            .Options;

        try
        {
            var factory = new TestContextFactory(options);
            await using (var context = await factory.CreateDbContextAsync())
            {
                await context.Database.MigrateAsync();
                var now = DateTime.UtcNow;
                var document = new DocumentEntity
                {
                    RefNumber = "INV-AP-1",
                    TypeCode = "Invoice",
                    Company = new CompanyEntity { Uen = "UEN-1", Name = "Supplier" },
                    SentDate = now,
                    CreatedTime = now,
                    ModifiedTime = now,
                    ApJournal = new DocumentAPJournalEntity
                    {
                        Confidence = 0.81m,
                        GstCode = "SG_UNKNOWN",
                        CoaSuggestionsJson = JsonSerializer.Serialize(
                            new[] { new AccountsPayableLineSuggestion(1, "6000", "Office supplies", true) }),
                        VerificationNotes = "Invoice totals do not reconcile"
                    }
                };
                context.Documents.Add(document);
                await context.SaveChangesAsync(TestContext.Current.CancellationToken);

                var journal = await new AccountsPayableService(factory)
                    .GetCodingAsync(document.Id, TestContext.Current.CancellationToken);

                Assert.Equal(0.81m, journal!.Confidence);
                Assert.Equal("SG_UNKNOWN", journal.GstCode);
                Assert.True(Assert.Single(journal.CoaSuggestions).RequiresReview);
                Assert.Equal("Invoice totals do not reconcile", Assert.Single(journal.VerificationNotes));
            }
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
