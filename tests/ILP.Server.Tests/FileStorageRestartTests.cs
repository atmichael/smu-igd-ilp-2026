using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace ILP.Server.Tests;

/// <summary>Runs the API on the real file stores in a temporary folder and restarts it between steps.</summary>
public sealed class FileStorageRestartTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"ilp-restart-{Guid.NewGuid():N}");

    [Fact]
    public async Task IntakeAndEvidence_SurviveApiRestart()
    {
        var idempotencyKey = Guid.NewGuid().ToString();
        string sourceDocumentId;
        string packageId;

        using (var firstRun = StartApi())
        {
            var client = Client(firstRun, idempotencyKey);
            var intake = await (await client.PostAsync("/api/source-documents", CameraCapture())).Content.ReadFromJsonAsync<JsonElement>();
            sourceDocumentId = intake.GetProperty("sourceDocumentId").GetString()!;

            var created = await client.PostAsJsonAsync("/api/evidence-packages", new
            {
                caseId = "AP-CASE-RESTART",
                documents = new[] { new { documentType = "invoice", sourceReference = "INV-RESTART-1", sourceDocumentId } }
            });
            packageId = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("evidencePackageId").GetString()!;
            Assert.Equal(HttpStatusCode.OK, (await client.PostAsync($"/api/evidence-packages/{packageId}/finalize", null)).StatusCode);
        }

        using var secondRun = StartApi();
        var restarted = Client(secondRun, idempotencyKey);

        var package = await restarted.GetFromJsonAsync<JsonElement>($"/api/evidence-packages/{packageId}");
        Assert.Equal("confirmed", package.GetProperty("reviewStatus").GetString());
        Assert.Equal($"protected://source-documents/{sourceDocumentId}", package.GetProperty("documents")[0].GetProperty("storageLocation").GetString());

        Assert.Equal(HttpStatusCode.OK, (await restarted.GetAsync($"/api/source-documents/{sourceDocumentId}")).StatusCode);
        Assert.Equal(JpegBytes, await restarted.GetByteArrayAsync($"/api/source-documents/{sourceDocumentId}/pages/1"));

        var retry = await restarted.PostAsync("/api/source-documents", CameraCapture());
        Assert.Equal(sourceDocumentId, (await retry.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("sourceDocumentId").GetString());
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    private static readonly byte[] JpegBytes = [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10];

    private WebApplicationFactory<Program> StartApi() =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("EvidenceStorage:Provider", "File");
            builder.UseSetting("EvidenceStorage:ContentProvider", "File");
            builder.UseSetting("EvidenceStorage:RootPath", Path.Combine(_root, "evidence-store"));
            builder.UseSetting("EvidenceStorage:ContentRootPath", Path.Combine(_root, "source-documents"));
            builder.UseSetting("EvidenceStorage:SourceDocumentRecordsPath", Path.Combine(_root, "source-document-records"));
        });

    private static HttpClient Client(WebApplicationFactory<Program> api, string idempotencyKey)
    {
        var client = api.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test", "restart");
        client.DefaultRequestHeaders.Add("Idempotency-Key", idempotencyKey);
        return client;
    }

    private static MultipartFormDataContent CameraCapture()
    {
        var content = new MultipartFormDataContent { { new StringContent("camera-capture"), "channel" } };
        var page = new ByteArrayContent(JpegBytes);
        page.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        content.Add(page, "pages", "page-1.jpg");
        return content;
    }
}
