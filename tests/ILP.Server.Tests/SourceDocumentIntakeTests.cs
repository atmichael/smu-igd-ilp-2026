using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace ILP.Server.Tests;

public class SourceDocumentIntakeTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public SourceDocumentIntakeTests(WebApplicationFactory<Program> factory)
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

    private sealed class SourceDocumentResponse
    {
        public string SourceDocumentId { get; set; } = string.Empty;
        public string Channel { get; set; } = string.Empty;
        public int PageCount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string MediaType { get; set; } = string.Empty;
        public string? SubmittedBy { get; set; }
        public string ContentHash { get; set; } = string.Empty;
    }
}
