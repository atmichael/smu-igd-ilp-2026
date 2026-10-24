using ILP.Server.Features.InboxMonitoring.Persistence;
using ILP.Shared.InboxMonitoring.Dto;
using ILP.Shared.TransactionDocument.Dto;
using Microsoft.Data.Sqlite;
using Xunit;

namespace ILP.Server.Tests;

public sealed class InboxExtractionPersistenceTests
{
    [Fact]
    public async Task SaveAsync_PersistsNormalizedRecordsAndTheirSourceLinks()
    {
        string connectionString = $"Data Source={Guid.NewGuid():N};Mode=Memory;Cache=Shared";
        using var persistence = new SqliteInboxExtractionPersistence(connectionString);
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
            company: new CompanyDto("Supplier", "UEN-21"),
            confidence: 0.91m);

        await persistence.SaveAsync([new DocumentInboxMessageMappingDto(document, message, attachment)]);

        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT d.RefNumber, d.Confidence, c.Uen, i.Description, m.UniqueId, a.WebViewLink,
                   mapping.SourceAttachmentId IS NOT NULL
            FROM Documents d
            JOIN Companies c ON c.Id = d.CompanyId
            JOIN DocumentItems i ON i.DocumentId = d.Id
            JOIN DocumentInboxMessages mapping ON mapping.DocumentId = d.Id
            JOIN InboxMessages m ON m.Id = mapping.MessageId
            JOIN InboxMessageAttachments a ON a.Id = mapping.SourceAttachmentId
            """;

        await using var reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        Assert.Equal("INV-21", reader.GetString(0));
        Assert.Equal("0.91", reader.GetString(1));
        Assert.Equal("UEN-21", reader.GetString(2));
        Assert.Equal("Parts", reader.GetString(3));
        Assert.Equal(21, reader.GetInt64(4));
        Assert.Equal(attachment.WebViewLink, reader.GetString(5));
        Assert.True(reader.GetBoolean(6));
        Assert.False(await reader.ReadAsync());

        await reader.DisposeAsync();
        await using var auditCommand = connection.CreateCommand();
        auditCommand.CommandText = """
            SELECT COUNT(*) FROM (
                SELECT Version, ActiveStatus, CreatedTime, ModifiedTime FROM Companies
                UNION ALL SELECT Version, ActiveStatus, CreatedTime, ModifiedTime FROM Documents
                UNION ALL SELECT Version, ActiveStatus, CreatedTime, ModifiedTime FROM DocumentItems
                UNION ALL SELECT Version, ActiveStatus, CreatedTime, ModifiedTime FROM InboxMessages
                UNION ALL SELECT Version, ActiveStatus, CreatedTime, ModifiedTime FROM InboxMessageAttachments
                UNION ALL SELECT Version, ActiveStatus, CreatedTime, ModifiedTime FROM DocumentInboxMessages
            ) WHERE Version = 1 AND ActiveStatus = 1 AND length(CreatedTime) > 0 AND length(ModifiedTime) > 0
            """;
        Assert.Equal(6L, (long)(await auditCommand.ExecuteScalarAsync())!);
    }
}
