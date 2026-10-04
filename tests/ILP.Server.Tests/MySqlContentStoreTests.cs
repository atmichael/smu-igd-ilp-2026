using ILP.Server.Features.EvidenceStorage;
using MySqlConnector;
using Xunit;

namespace ILP.Server.Tests;

/// <summary>Runs against a real MySQL server: start it from setup/docker-compose.yml and set ILP_TEST_MYSQL.</summary>
public sealed class MySqlContentStoreTests
{
    [MySqlFact]
    public async Task SaveReadAndExists_RoundTripWithContentType()
    {
        var connectionString = Environment.GetEnvironmentVariable(MySqlFactAttribute.ConnectionVariable)!;
        var store = new MySqlDocumentContentStore(connectionString);
        var id = Guid.NewGuid().ToString();
        byte[] pdf = [0x25, 0x50, 0x44, 0x46, 0x2D];

        var location = await store.SaveAsync(id, [new DocumentContentPart(@"..\original.pdf", pdf, "application/pdf")]);

        Assert.Equal($"protected://source-documents/{id}", location);
        Assert.True(await store.ExistsAsync(id));
        Assert.Equal(pdf, await store.ReadAsync(id, "original.pdf"));
        Assert.Null(await store.ReadAsync(id, "page-1.jpg"));
        Assert.False(await store.ExistsAsync(Guid.NewGuid().ToString()));
        Assert.False(await store.ExistsAsync(@"..\..\secrets"));

        await using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new MySqlCommand(
            "SELECT content_type FROM source_document_content WHERE source_document_id = @id AND file_name = 'original.pdf'", connection);
        command.Parameters.AddWithValue("@id", id);
        Assert.Equal("application/pdf", await command.ExecuteScalarAsync());
    }

    [MySqlFact]
    public async Task Save_WhenAPartFails_StoresNothing()
    {
        var store = new MySqlDocumentContentStore(Environment.GetEnvironmentVariable(MySqlFactAttribute.ConnectionVariable)!);
        var id = Guid.NewGuid().ToString();
        var page = new DocumentContentPart("page-1.jpg", [0xFF, 0xD8, 0xFF], "image/jpeg");

        await Assert.ThrowsAsync<MySqlException>(() => store.SaveAsync(id, [page, page]));

        Assert.False(await store.ExistsAsync(id));
    }
}

public sealed class MySqlFactAttribute : FactAttribute
{
    public const string ConnectionVariable = "ILP_TEST_MYSQL";

    public MySqlFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(ConnectionVariable)))
        {
            Skip = $"Set {ConnectionVariable} to a MySQL connection string (MySQL from setup/docker-compose.yml) to run.";
        }
    }
}
