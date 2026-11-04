using ILP.Server.Features.AccountsPayable.Service;
using ILP.Server.Features.InboxMonitoring.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
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

    private sealed class TestContextFactory(DbContextOptions<InboxExtractionDbContext> options)
        : IDbContextFactory<InboxExtractionDbContext>
    {
        public InboxExtractionDbContext CreateDbContext() => new(options);

        public Task<InboxExtractionDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(CreateDbContext());
    }
}
