using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;

namespace ILP.Server.Config;

public static class InboxExtractionPersistenceConfig
{
    public const string SectionName = "InboxExtractionPersistence";

    public static string DatabasePath { get; private set; } = "";

    public static string ConnectionString { get; private set; } = "";

    public static void Initialize(IConfiguration configuration, string contentRootPath)
    {
        string? configuredPath = configuration[$"{SectionName}:DatabasePath"];
        if (string.IsNullOrWhiteSpace(configuredPath))
        {
            throw new ApplicationException($"Missing mandatory: {SectionName}:DatabasePath");
        }

        DatabasePath = Path.GetFullPath(configuredPath, contentRootPath);
        string? directory = Path.GetDirectoryName(DatabasePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        ConnectionString = new SqliteConnectionStringBuilder { DataSource = DatabasePath }.ToString();
    }
}
