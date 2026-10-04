using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace ILP.Server.Tests;

public class EvidenceStorageTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public EvidenceStorageTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task PostEvidencePackage_WithValidPayload_ReturnsCreated()
    {
        var client = _factory.CreateClient();
        var request = CreateEvidencePackageRequest("AP-CASE-2048", "mixed", "draft");

        var response = await client.PostAsJsonAsync("/api/evidence-packages", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<EvidencePackageResponse>();
        Assert.NotNull(payload);
        Assert.Equal("AP-CASE-2048", payload.CaseId);
        Assert.Equal("draft", payload.Status);
        Assert.NotNull(payload.EvidencePackageId);
    }

    [Fact]
    public async Task GetEvidencePackages_ByCaseId_ReturnsMatchingPackage()
    {
        var client = _factory.CreateClient();
        var request = CreateEvidencePackageRequest("AP-CASE-2001", "mixed", "draft");

        var post = await client.PostAsJsonAsync("/api/evidence-packages", request);
        Assert.Equal(HttpStatusCode.Created, post.StatusCode);

        var response = await client.GetAsync("/api/evidence-packages?caseId=AP-CASE-2001");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<List<EvidencePackageResponse>>();
        Assert.NotNull(payload);
        Assert.Contains(payload, p => p.CaseId == "AP-CASE-2001");
    }

    [Fact]
    public async Task PostEvidencePackage_WithDuplicateFinalSourceAndCase_ReturnsConflict()
    {
        var client = _factory.CreateClient();
        var request = CreateEvidencePackageRequest("AP-CASE-3001", "mixed", "confirmed");

        var first = await client.PostAsJsonAsync("/api/evidence-packages", request);
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var second = await client.PostAsJsonAsync("/api/evidence-packages", request);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task PostEvidencePackage_WithProvenance_RetainsHistoricalValues()
    {
        var client = _factory.CreateClient();
        var request = CreateEvidencePackageRequest("AP-CASE-4001", "mixed", "confirmed");

        var response = await client.PostAsJsonAsync("/api/evidence-packages", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<EvidencePackageResponse>();
        Assert.NotNull(payload);

        var retrieved = await client.GetAsync($"/api/evidence-packages/{payload.EvidencePackageId}");
        Assert.Equal(HttpStatusCode.OK, retrieved.StatusCode);

        var package = await retrieved.Content.ReadFromJsonAsync<EvidencePackageResponse>();
        Assert.NotNull(package);
        Assert.NotEmpty(package.Documents);
        Assert.Equal("1450.00", package.Documents[0].Records[0].RawValue);
        Assert.Equal("1495.00", package.Documents[0].Records[0].CurrentValue);
        Assert.Equal(2, package.Documents[0].Records[0].Provenance.Count);
    }

    private static object CreateEvidencePackageRequest(string caseId, string sourceType, string reviewStatus)
    {
        return new
        {
            caseId,
            sourceType,
            reviewStatus,
            documents = new[]
            {
                new
                {
                    documentType = "invoice",
                    sourceReference = "INV-10492",
                    reviewStatus = "confirmed",
                    storageLocation = "protected://evidence/invoices/INV-10492.pdf",
                    checksum = "sha256:abc123",
                    records = new[]
                    {
                        new
                        {
                            recordCategory = "invoice-line",
                            recordType = "amount",
                            rawValue = "1450.00",
                            currentValue = "1495.00",
                            reviewStatus = "confirmed",
                            provenance = new[]
                            {
                                new
                                {
                                    eventType = "extracted",
                                    actorType = "model",
                                    previousValue = (string?)null,
                                    newValue = "1450.00",
                                    sourceReference = "ocr/invoice-10492"
                                },
                                new
                                {
                                    eventType = "corrected",
                                    actorType = "user",
                                    previousValue = "1450.00",
                                    newValue = "1495.00",
                                    reason = "supplier confirmed revised amount"
                                }
                            }
                        }
                    }
                }
            },
            auditEvents = new[]
            {
                new
                {
                    eventType = "source-attached",
                    actorType = "system",
                    message = "Invoice document retained with package linkage",
                    metadata = new { sourceReference = "INV-10492", schemaVersion = "ap-evidence-v1" }
                }
            }
        };
    }

    private sealed class EvidencePackageResponse
    {
        public string EvidencePackageId { get; set; } = string.Empty;
        public string CaseId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public List<DocumentResponse> Documents { get; set; } = new();
    }

    private sealed class DocumentResponse
    {
        public string SourceReference { get; set; } = string.Empty;
        public List<RecordResponse> Records { get; set; } = new();
    }

    private sealed class RecordResponse
    {
        public string RawValue { get; set; } = string.Empty;
        public string CurrentValue { get; set; } = string.Empty;
        public List<ProvenanceResponse> Provenance { get; set; } = new();
    }

    private sealed class ProvenanceResponse
    {
        public string EventType { get; set; } = string.Empty;
        public string ActorType { get; set; } = string.Empty;
        public string? PreviousValue { get; set; }
        public string? NewValue { get; set; }
    }
}
