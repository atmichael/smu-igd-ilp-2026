using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ILP.Server.Features.EvidenceStorage;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace ILP.Server.Tests;

public class SourceDocumentIntakeTests : IClassFixture<EvidenceApiFactory>
{
    private static readonly byte[] JpegBytes = [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00];
    private readonly EvidenceApiFactory _factory;

    public SourceDocumentIntakeTests(EvidenceApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task PostSourceDocuments_WithValidCameraCapturePayload_ReturnsCreated()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test", "camera-capture");

        var content = new MultipartFormDataContent();
        content.Add(new StringContent("camera-capture"), "channel");

        var jpegBytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00 };
        var pagePart = new ByteArrayContent(jpegBytes);
        pagePart.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
        content.Add(pagePart, "pages", "page-1.jpg");

        client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString());

        var response = await client.PostAsync("/api/source-documents", content);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<SourceDocumentResponse>();
        Assert.NotNull(payload);
        Assert.Equal("camera-capture", payload.Channel);
        Assert.Equal(1, payload.PageCount);
        Assert.Equal("image/jpeg", payload.MediaType);
        Assert.Equal("camera-capture-test-user", payload.SubmittedBy);
        Assert.StartsWith("sha256:", payload.ContentHash);
        Assert.Equal($"protected://source-documents/{payload.SourceDocumentId}", payload.StorageLocation);
        var stored = Assert.Single(_factory.Content.Get(payload.SourceDocumentId)!);
        Assert.Equal("page-1.jpg", stored.FileName);
        Assert.Equal(jpegBytes, stored.Content);
    }

    [Fact]
    public async Task PostSourceDocuments_WhenContentCannotBeStored_ReturnsFailureAndRetrySucceeds()
    {
        var failingStore = new FailOnceContentStore();
        var client = _factory
            .WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IDocumentContentStore>();
                services.AddSingleton<IDocumentContentStore>(failingStore);
            }))
            .CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test", "camera-capture");
        client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString());

        var failed = await client.PostAsync("/api/source-documents", CameraCapture());
        var retried = await client.PostAsync("/api/source-documents", CameraCapture());

        Assert.Equal(HttpStatusCode.InternalServerError, failed.StatusCode);
        Assert.Equal(HttpStatusCode.Created, retried.StatusCode);
    }

    [Fact]
    public async Task EvidenceDocument_ReferencingStoredIntake_GetsItsStorageLocation()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test", "camera-capture");
        client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString());
        var intake = await (await client.PostAsync("/api/source-documents", CameraCapture()))
            .Content.ReadFromJsonAsync<SourceDocumentResponse>();

        var response = await client.PostAsJsonAsync("/api/evidence-packages", new
        {
            caseId = $"AP-CASE-{Guid.NewGuid():N}",
            documents = new[]
            {
                new { documentType = "invoice", sourceReference = "INV-INTAKE-1", sourceDocumentId = intake!.SourceDocumentId }
            }
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var package = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(intake.StorageLocation, package.GetProperty("documents")[0].GetProperty("storageLocation").GetString());
    }

    [Fact]
    public async Task GetSourceDocument_ReturnsRecordAndOriginalPages()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test", "reviewer");
        client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString());
        var intake = await (await client.PostAsync("/api/source-documents", CameraCapture()))
            .Content.ReadFromJsonAsync<SourceDocumentResponse>();

        var record = await client.GetFromJsonAsync<SourceDocumentResponse>($"/api/source-documents/{intake!.SourceDocumentId}");
        var page = await client.GetAsync($"/api/source-documents/{intake.SourceDocumentId}/pages/1");
        var missingPage = await client.GetAsync($"/api/source-documents/{intake.SourceDocumentId}/pages/2");
        var unknown = await client.GetAsync($"/api/source-documents/{Guid.NewGuid()}");
        var traversal = await client.GetAsync("/api/source-documents/..%2F..%2Fsecrets/pages/1");

        Assert.Equal(intake.StorageLocation, record!.StorageLocation);
        Assert.Equal(HttpStatusCode.OK, page.StatusCode);
        Assert.Equal("image/jpeg", page.Content.Headers.ContentType?.MediaType);
        Assert.Equal(JpegBytes, await page.Content.ReadAsByteArrayAsync());
        Assert.Equal(HttpStatusCode.NotFound, missingPage.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, traversal.StatusCode);
    }

    [Fact]
    public async Task GetSourceDocumentPage_WithoutAuthentication_IsUnauthorized()
    {
        var response = await _factory.CreateClient().GetAsync($"/api/source-documents/{Guid.NewGuid()}/pages/1");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void FileSourceDocumentRepository_KeepsRecordsAndIdempotencyKeysAcrossInstances()
    {
        var root = Path.Combine(Path.GetTempPath(), $"ilp-intake-{Guid.NewGuid():N}");
        try
        {
            var document = new ILP.Shared.SourceDocuments.SourceDocumentMetadata { PageCount = 1 };
            new FileSourceDocumentRepository(root).Save(new SourceDocumentRecord(document, "key-1", "hash-1"));

            var reopened = new FileSourceDocumentRepository(root);

            Assert.Equal(1, reopened.Get(document.SourceDocumentId)!.Document.PageCount);
            Assert.Equal(document.SourceDocumentId, reopened.FindByIdempotencyKey("key-1")!.Document.SourceDocumentId);
            Assert.Null(reopened.Get(@"..\..\secrets"));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task PostSourceDocuments_WithLegacySourceFieldOnly_IsRejected()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test", "camera-capture");
        client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString());

        var content = new MultipartFormDataContent();
        content.Add(new StringContent("camera-capture"), "source");
        var pagePart = new ByteArrayContent([0xFF, 0xD8, 0xFF, 0xE0]);
        pagePart.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        content.Add(pagePart, "pages", "page-1.jpg");

        var response = await client.PostAsync("/api/source-documents", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostSourceDocuments_RetryWithSameKeyAndPayload_ReturnsOriginalDocument()
    {
        var client = AuthorizedClient(Guid.NewGuid().ToString());

        var first = await (await client.PostAsync("/api/source-documents", CameraCapture())).Content.ReadFromJsonAsync<SourceDocumentResponse>();
        var retry = await client.PostAsync("/api/source-documents", CameraCapture());

        Assert.Equal(HttpStatusCode.Created, retry.StatusCode);
        Assert.Equal(first!.SourceDocumentId, (await retry.Content.ReadFromJsonAsync<SourceDocumentResponse>())!.SourceDocumentId);
    }

    [Fact]
    public async Task PostSourceDocuments_SameKeyWithDifferentPayload_ReturnsConflict()
    {
        var client = AuthorizedClient(Guid.NewGuid().ToString());
        await client.PostAsync("/api/source-documents", CameraCapture());

        var response = await client.PostAsync("/api/source-documents", CameraCapture(JpegBytes, JpegBytes));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task PostSourceDocuments_WithMultiplePages_StoresThemInOrder()
    {
        byte[] second = [0xFF, 0xD8, 0xFF, 0xE1, 0x02];
        var client = AuthorizedClient(Guid.NewGuid().ToString());

        var intake = await (await client.PostAsync("/api/source-documents", CameraCapture(JpegBytes, second)))
            .Content.ReadFromJsonAsync<SourceDocumentResponse>();

        Assert.Equal(2, intake!.PageCount);
        Assert.Equal(second, await client.GetByteArrayAsync($"/api/source-documents/{intake.SourceDocumentId}/pages/2"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    public async Task PostSourceDocuments_WithPageCountOutsideOneToThree_ReturnsBadRequest(int pageCount)
    {
        var client = AuthorizedClient(Guid.NewGuid().ToString());

        var response = await client.PostAsync("/api/source-documents", CameraCapture(Enumerable.Repeat(JpegBytes, pageCount).ToArray()));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostSourceDocuments_WithoutIdempotencyKey_ReturnsBadRequest()
    {
        var client = AuthorizedClient(idempotencyKey: null);

        var response = await client.PostAsync("/api/source-documents", CameraCapture());

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("image/png", new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 })]
    [InlineData("image/jpeg", new byte[] { 0x89, 0x50, 0x4E, 0x47 })]
    public async Task PostSourceDocuments_WithNonJpegPage_ReturnsUnsupportedMediaTypeAndStoresNothing(string contentType, byte[] bytes)
    {
        var client = AuthorizedClient(Guid.NewGuid().ToString());
        var content = new MultipartFormDataContent { { new StringContent("camera-capture"), "channel" } };
        var page = new ByteArrayContent(bytes);
        page.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        content.Add(page, "pages", "page-1.jpg");

        var response = await client.PostAsync("/api/source-documents", content);

        Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
    }

    private HttpClient AuthorizedClient(string? idempotencyKey)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test", "camera-capture");
        if (idempotencyKey is not null)
        {
            client.DefaultRequestHeaders.Add("Idempotency-Key", idempotencyKey);
        }

        return client;
    }

    private static MultipartFormDataContent CameraCapture(params byte[][] pages)
    {
        var content = new MultipartFormDataContent();
        content.Add(new StringContent("camera-capture"), "channel");
        foreach (var bytes in pages)
        {
            var page = new ByteArrayContent(bytes);
            page.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
            content.Add(page, "pages", "page.jpg");
        }

        return content;
    }

    private static MultipartFormDataContent CameraCapture()
    {
        var content = new MultipartFormDataContent();
        content.Add(new StringContent("camera-capture"), "channel");
        var page = new ByteArrayContent(JpegBytes);
        page.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        content.Add(page, "pages", "page-1.jpg");
        return content;
    }

    private sealed class FailOnceContentStore : InMemoryDocumentContentStore
    {
        private int _calls;

        public override string Save(string sourceDocumentId, IReadOnlyList<DocumentContentPart> parts) =>
            Interlocked.Increment(ref _calls) == 1
                ? throw new IOException("Simulated storage failure.")
                : base.Save(sourceDocumentId, parts);
    }

    private sealed class SourceDocumentResponse
    {
        public string SourceDocumentId { get; set; } = string.Empty;
        public string Channel { get; set; } = string.Empty;
        public int PageCount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string MediaType { get; set; } = string.Empty;
        public string? SubmittedBy { get; set; }
        public string ContentHash { get; set; } = string.Empty;
        public string StorageLocation { get; set; } = string.Empty;
    }
}
