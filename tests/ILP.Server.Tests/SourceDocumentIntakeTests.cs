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
        content.Add(new StringContent("camera-capture"), "source");

        var jpegBytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00 };
        var pagePart = new ByteArrayContent(jpegBytes);
        pagePart.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
        content.Add(pagePart, "pages", "page-1.jpg");

        client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString());

        var response = await client.PostAsync("/api/source-documents", content);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<SourceDocumentResponse>();
        Assert.NotNull(payload);
        Assert.Equal("camera-capture", payload.Source);
        Assert.Equal(1, payload.PageCount);
    }

    private sealed class SourceDocumentResponse
    {
        public string SourceDocumentId { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public int PageCount { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
