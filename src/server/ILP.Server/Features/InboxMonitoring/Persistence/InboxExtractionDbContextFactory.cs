using ILP.Server.Config;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ILP.Server.Features.InboxMonitoring.Persistence;

public sealed class InboxExtractionDbContextFactory : IDesignTimeDbContextFactory<InboxExtractionDbContext>
{
    public InboxExtractionDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddEnvironmentVariables()
            .Build();
        InboxExtractionPersistenceConfig.Initialize(configuration, Directory.GetCurrentDirectory());

        var options = new DbContextOptionsBuilder<InboxExtractionDbContext>()
            .UseSqlite(InboxExtractionPersistenceConfig.ConnectionString)
            .Options;
        return new InboxExtractionDbContext(options);
    }
}
