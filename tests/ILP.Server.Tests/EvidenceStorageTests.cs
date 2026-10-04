using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using ILP.Server.Features.EvidenceStorage;
using ILP.Shared.Evidence;
using ILP.Shared.InfoExtraction;
using Xunit;

namespace ILP.Server.Tests;

public class EvidenceStorageTests : IClassFixture<EvidenceApiFactory>
{
    private const string BasePath = "/api/evidence-packages";
    private const string TestActor = "camera-capture-test-user";
    private readonly EvidenceApiFactory _factory;
    private readonly HttpClient _client;

    public EvidenceStorageTests(EvidenceApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test", "evidence-reviewer");
    }

    // ---- US1: create, retrieve, query ----

    [Fact]
    public async Task Create_WithValidDraft_ReturnsCreatedWithLinkedIdentifiersAndServerAudit()
    {
        var response = await _client.PostAsJsonAsync(BasePath, PackageJson(NewCase()));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var package = await ReadJson(response);
        var packageId = package.GetProperty("evidencePackageId").GetString();
        Assert.Equal("draft", package.GetProperty("reviewStatus").GetString());
        Assert.False(package.TryGetProperty("status", out _));
        Assert.Equal("invoice", package.GetProperty("packageType").GetString());
        Assert.Equal($"{BasePath}/{packageId}", response.Headers.Location?.ToString());

        var document = package.GetProperty("documents")[0];
        Assert.Equal(packageId, document.GetProperty("evidencePackageId").GetString());
        var record = document.GetProperty("records")[0];
        Assert.Equal(document.GetProperty("documentId").GetString(), record.GetProperty("documentId").GetString());

        var auditTypes = EventTypes(package);
        Assert.Contains("source-attached", auditTypes);
        Assert.Contains("model-versioned", auditTypes);
        Assert.Contains("schema-versioned", auditTypes);
        Assert.Contains("user-correction", auditTypes);
    }

    [Fact]
    public async Task Create_WithClientAuditEvent_RecordsAuthenticatedCallerAsActor()
    {
        var request = PackageJson(NewCase());
        request["auditEvents"] = new JsonArray(new JsonObject
        {
            ["eventType"] = "source-attached",
            ["actorType"] = "user",
            ["actorId"] = "someone-else",
            ["message"] = "Invoice attached by intake."
        });

        var package = await CreateAsync(request);

        var clientEvent = package.GetProperty("auditEvents").EnumerateArray()
            .Single(audit => audit.GetProperty("message").GetString() == "Invoice attached by intake.");
        Assert.Equal(TestActor, clientEvent.GetProperty("actorId").GetString());
        Assert.Equal("someone-else", clientEvent.GetProperty("metadata").GetProperty("claimedActorId").GetString());
    }

    [Fact]
    public async Task Create_WithClientAuditEventContainingValuesAndCredentials_RedactsThem()
    {
        var request = PackageJson(NewCase());
        request["auditEvents"] = new JsonArray(new JsonObject
        {
            ["eventType"] = "user-correction",
            ["actorType"] = "user",
            ["message"] = "Corrected to 1495.00 using token=abc123",
            ["metadata"] = new JsonObject { ["apiKey"] = "xyz", ["note"] = "was 1450.00" }
        });

        var package = await CreateAsync(request);

        var clientEvent = package.GetProperty("auditEvents").EnumerateArray()
            .Single(audit => audit.GetProperty("message").GetString()!.StartsWith("Corrected to"));
        var message = clientEvent.GetProperty("message").GetString()!;
        Assert.DoesNotContain("1495.00", message);
        Assert.DoesNotContain("abc123", message);
        var metadata = clientEvent.GetProperty("metadata");
        Assert.Equal(SensitiveDataGuard.Redacted, metadata.GetProperty("apiKey").GetString());
        Assert.Equal($"was {SensitiveDataGuard.Redacted}", metadata.GetProperty("note").GetString());
    }

    [Fact]
    public async Task Create_FromExtractedDocumentHeader_StoresHeaderRecordsKeyedByExtractionKeys()
    {
        var fields = DocumentHeaderParser.Parse("document-number| INV-77\ndocument-type| Invoice\ntotal-amount| 1308.00");
        var request = new CreateEvidencePackageRequest
        {
            CaseId = NewCase(),
            Documents = [ExtractedDocumentMapper.ToEvidenceDocument(fields, "protected://evidence/inv-77.pdf", "sha256:abc")]
        };

        var package = await ReadJson(await _client.PostAsJsonAsync(BasePath, request));

        var document = package.GetProperty("documents")[0];
        Assert.Equal("INV-77", document.GetProperty("sourceReference").GetString());
        Assert.Contains(document.GetProperty("records").EnumerateArray(), record =>
            record.GetProperty("recordType").GetString() == DocumentHeaderFields.TotalAmount
            && record.GetProperty("recordCategory").GetString() == "document-header");
    }

    [Fact]
    public async Task Requests_WithoutAuthentication_AreRejected()
    {
        var anonymous = _factory.CreateClient();

        var create = await anonymous.PostAsJsonAsync(BasePath, PackageJson(NewCase()));
        var query = await anonymous.GetAsync($"{BasePath}?caseId=any");

        Assert.Equal(HttpStatusCode.Unauthorized, create.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, query.StatusCode);
    }

    [Theory]
    [InlineData("confirmed")]
    [InlineData("reviewed")]
    public async Task Create_WithFinalStatus_IsRejectedBecauseFinalizationIsRequired(string status)
    {
        var request = PackageJson(NewCase());
        request["reviewStatus"] = status;

        var response = await _client.PostAsJsonAsync(BasePath, request);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var problem = await ReadJson(response);
        Assert.True(problem.GetProperty("errors").TryGetProperty("reviewStatus", out _));
    }

    [Fact]
    public async Task Create_WithoutCaseId_ReturnsBadRequest()
    {
        var request = PackageJson(NewCase());
        request["caseId"] = "";

        var response = await _client.PostAsJsonAsync(BasePath, request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithInvalidEnumsAndMixedCategories_ReturnsValidationErrors()
    {
        var request = PackageJson(NewCase());
        var record = request["documents"]![0]!["records"]![0]!;
        record["recordCategory"] = "purchase-order-commitment";
        record["verificationStatus"] = "maybe";
        request["documents"]![0]!["reviewStatus"] = "unknown-status";

        var response = await _client.PostAsJsonAsync(BasePath, request);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var errors = (await ReadJson(response)).GetProperty("errors");
        Assert.True(errors.TryGetProperty("documents[0].records[0].recordCategory", out _));
        Assert.True(errors.TryGetProperty("documents[0].records[0].verificationStatus", out _));
        Assert.True(errors.TryGetProperty("documents[0].reviewStatus", out _));
    }

    [Fact]
    public async Task Query_ByCaseDocumentIntakeSourceReferenceAndStatus_ReturnsPackage()
    {
        var caseId = NewCase();
        var sourceReference = $"INV-{Guid.NewGuid():N}";
        var intakeId = Guid.NewGuid().ToString();
        var request = PackageJson(caseId, sourceReference);
        request["documents"]![0]!["sourceDocumentId"] = intakeId;
        var created = await CreateAsync(request);
        var packageId = created.GetProperty("evidencePackageId").GetString()!;
        var documentId = created.GetProperty("documents")[0].GetProperty("documentId").GetString();

        foreach (var query in new[]
        {
            $"caseId={caseId}",
            $"documentId={documentId}",
            $"documentId={intakeId}",
            $"sourceReference={sourceReference}",
            $"caseId={caseId}&reviewStatus=draft"
        })
        {
            var response = await _client.GetAsync($"{BasePath}?{query}");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var results = await ReadJson(response);
            Assert.Contains(results.EnumerateArray(), package => package.GetProperty("evidencePackageId").GetString() == packageId);
        }

        var none = await ReadJson(await _client.GetAsync($"{BasePath}?caseId={caseId}&reviewStatus=confirmed"));
        Assert.Empty(none.EnumerateArray());

        var invalid = await _client.GetAsync($"{BasePath}?reviewStatus=bogus");
        Assert.Equal(HttpStatusCode.UnprocessableEntity, invalid.StatusCode);
    }

    [Fact]
    public async Task Get_UnknownPackage_ReturnsNotFound()
    {
        var response = await _client.GetAsync($"{BasePath}/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ---- Finalization, deduplication, replacement ----

    [Fact]
    public async Task Finalize_DraftPackage_ConfirmsEvidenceWithRetentionAndRejectsSecondFinalize()
    {
        var packageId = await CreateIdAsync(PackageJson(NewCase()));

        var response = await _client.PostAsync($"{BasePath}/{packageId}/finalize", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var package = await ReadJson(response);
        Assert.Equal("confirmed", package.GetProperty("reviewStatus").GetString());
        var finalizedAt = package.GetProperty("finalizedAt").GetDateTimeOffset();
        Assert.Equal(finalizedAt.AddYears(3), package.GetProperty("retentionUntil").GetDateTimeOffset());
        var record = package.GetProperty("documents")[0].GetProperty("records")[0];
        Assert.Equal("confirmed", record.GetProperty("reviewStatus").GetString());
        Assert.Equal("finalized", LastProvenanceEntry(record).GetProperty("eventType").GetString());
        Assert.Contains("finalized", EventTypes(package));

        var again = await _client.PostAsync($"{BasePath}/{packageId}/finalize", null);
        Assert.Equal(HttpStatusCode.PreconditionFailed, again.StatusCode);
    }

    [Fact]
    public async Task Finalize_UnknownPackage_ReturnsNotFound()
    {
        var response = await _client.PostAsync($"{BasePath}/{Guid.NewGuid()}/finalize", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Replacement_OfDraftPackage_IsRejected()
    {
        var caseId = NewCase();
        var draftId = await CreateIdAsync(PackageJson(caseId));
        var request = PackageJson(caseId);
        request["replacesEvidencePackageId"] = draftId;
        request["replacementReason"] = "Supplier reissued the invoice";

        var response = await _client.PostAsJsonAsync(BasePath, request);

        Assert.Equal(HttpStatusCode.PreconditionFailed, response.StatusCode);
    }

    [Fact]
    public async Task Replacement_ForDifferentCase_IsRejected()
    {
        var confirmedId = (await FinalizeAsync(await CreateIdAsync(PackageJson(NewCase())))).GetProperty("evidencePackageId").GetString();
        var request = PackageJson(NewCase());
        request["replacesEvidencePackageId"] = confirmedId;
        request["replacementReason"] = "Wrong case";

        var response = await _client.PostAsJsonAsync(BasePath, request);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.True((await ReadJson(response)).GetProperty("errors").TryGetProperty("replacesEvidencePackageId", out _));
    }

    [Fact]
    public async Task Replacement_OfUnknownPackage_ReturnsNotFound()
    {
        var request = PackageJson(NewCase());
        request["replacesEvidencePackageId"] = Guid.NewGuid().ToString();
        request["replacementReason"] = "Unknown prior version";

        var response = await _client.PostAsJsonAsync(BasePath, request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Verification_OnSupersededPackage_IsRejected()
    {
        var caseId = NewCase();
        var original = await FinalizeAsync(await CreateIdAsync(PackageJson(caseId)));
        var originalId = original.GetProperty("evidencePackageId").GetString()!;
        var replacement = PackageJson(caseId);
        replacement["replacesEvidencePackageId"] = originalId;
        replacement["replacementReason"] = "Supplier reissued the invoice";
        await FinalizeAsync(await CreateIdAsync(replacement));

        var response = await _client.PostAsJsonAsync($"{BasePath}/{originalId}/records/{FirstRecordId(original)}/verifications",
            new { verificationStatus = "passed" });

        Assert.Equal(HttpStatusCode.PreconditionFailed, response.StatusCode);
    }

    [Fact]
    public async Task Correction_OfUnknownRecord_ReturnsNotFound()
    {
        var packageId = await CreateIdAsync(PackageJson(NewCase()));

        var response = await _client.PostAsJsonAsync($"{BasePath}/{packageId}/records/{Guid.NewGuid()}/corrections",
            new { newValue = "1.00", reason = "No such record" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task FullLifecycle_AuditTrailCoversEveryActionWithTheAuthenticatedActor()
    {
        var created = await CreateAsync(PackageJson(NewCase()));
        var packageId = created.GetProperty("evidencePackageId").GetString()!;
        var recordId = FirstRecordId(created);

        await _client.PostAsJsonAsync($"{BasePath}/{packageId}/records/{recordId}/corrections", new { newValue = "1500.00", reason = "Supplier credit note" });
        await _client.PostAsJsonAsync($"{BasePath}/{packageId}/status", new { reviewStatus = "pending-review" });
        await _client.PostAsJsonAsync($"{BasePath}/{packageId}/records/{recordId}/verifications", new { verificationStatus = "passed" });
        await _client.PostAsJsonAsync($"{BasePath}/{packageId}/match-outcomes", new { matchReviewId = "MR-1", outcome = "matched", supportingRecordIds = new[] { recordId } });
        var finalized = await FinalizeAsync(packageId);

        var events = finalized.GetProperty("auditEvents").EnumerateArray().ToList();
        Assert.Superset(
            new HashSet<string?> { "source-attached", "user-correction", "status-changed", "verification-result", "match-outcome", "finalized" },
            events.Select(audit => audit.GetProperty("eventType").GetString()).ToHashSet());
        Assert.All(events, audit => Assert.Equal(TestActor, audit.GetProperty("actorId").GetString()));

        var record = finalized.GetProperty("documents")[0].GetProperty("records")[0];
        Assert.Equal("1450.00", record.GetProperty("rawValue").GetString());
        Assert.Equal("1500.00", record.GetProperty("currentValue").GetString());
        Assert.Equal(
            new[] { "extracted", "corrected", "corrected", "verified", "finalized" },
            record.GetProperty("provenance").EnumerateArray().Select(entry => entry.GetProperty("eventType").GetString()!).ToArray());
    }

    [Fact]
    public async Task Create_ForSourceAndCaseWithConfirmedEvidence_ReturnsConflict()
    {
        var caseId = NewCase();
        await FinalizeAsync(await CreateIdAsync(PackageJson(caseId)));

        var duplicate = await _client.PostAsJsonAsync(BasePath, PackageJson(caseId));

        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
    }

    [Fact]
    public async Task Finalize_SecondDraftForSameSourceAndCase_ReturnsConflict()
    {
        var caseId = NewCase();
        var first = await CreateIdAsync(PackageJson(caseId));
        var second = await CreateIdAsync(PackageJson(caseId));
        await FinalizeAsync(first);

        var response = await _client.PostAsync($"{BasePath}/{second}/finalize", null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var stored = await GetAsync(second);
        Assert.Equal("draft", stored.GetProperty("reviewStatus").GetString());
    }

    [Fact]
    public async Task Replacement_FinalizedWithReason_SupersedesPriorConfirmedVersion()
    {
        var caseId = NewCase();
        var original = await CreateIdAsync(PackageJson(caseId));
        await FinalizeAsync(original);

        var replacementRequest = PackageJson(caseId);
        replacementRequest["replacesEvidencePackageId"] = original;
        replacementRequest["replacementReason"] = "Supplier reissued invoice";
        var replacement = await CreateAsync(replacementRequest);
        var replacementId = replacement.GetProperty("evidencePackageId").GetString()!;
        Assert.Equal(2, replacement.GetProperty("version").GetInt32());

        var finalized = await FinalizeAsync(replacementId);
        Assert.Equal("confirmed", finalized.GetProperty("reviewStatus").GetString());

        var prior = await GetAsync(original);
        Assert.Equal("superseded", prior.GetProperty("reviewStatus").GetString());
        Assert.Equal(replacementId, prior.GetProperty("supersededByEvidencePackageId").GetString());
        var priorRecord = prior.GetProperty("documents")[0].GetProperty("records")[0];
        Assert.Equal("superseded", priorRecord.GetProperty("reviewStatus").GetString());
        Assert.Equal("superseded", LastProvenanceEntry(priorRecord).GetProperty("eventType").GetString());
        Assert.Equal("1495.00", priorRecord.GetProperty("currentValue").GetString());
        Assert.Contains("superseded", EventTypes(prior));
    }

    [Fact]
    public async Task Replacement_WithoutReason_ReturnsValidationError()
    {
        var caseId = NewCase();
        var original = await CreateIdAsync(PackageJson(caseId));
        await FinalizeAsync(original);
        var request = PackageJson(caseId);
        request["replacesEvidencePackageId"] = original;

        var response = await _client.PostAsJsonAsync(BasePath, request);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    // ---- US2: provenance, corrections, verification ----

    [Fact]
    public async Task Create_WithProvenance_RetainsExtractedAndCorrectedValues()
    {
        var package = await GetAsync(await CreateIdAsync(PackageJson(NewCase())));

        var record = package.GetProperty("documents")[0].GetProperty("records")[0];
        Assert.Equal("1450.00", record.GetProperty("rawValue").GetString());
        Assert.Equal("1495.00", record.GetProperty("currentValue").GetString());
        Assert.Equal(2, record.GetProperty("valueVersion").GetInt32());
        var history = record.GetProperty("provenance");
        Assert.Equal("extracted", history[0].GetProperty("eventType").GetString());
        Assert.Equal("corrected", history[1].GetProperty("eventType").GetString());
        Assert.Equal("1450.00", history[1].GetProperty("previousValue").GetString());
    }

    [Fact]
    public async Task Correction_AfterCreate_AppendsHistoryAndLinkedAuditWithoutOverwritingRawValue()
    {
        var created = await CreateAsync(PackageJson(NewCase()));
        var packageId = created.GetProperty("evidencePackageId").GetString()!;
        var recordId = FirstRecordId(created);

        var response = await _client.PostAsJsonAsync($"{BasePath}/{packageId}/records/{recordId}/corrections",
            new { newValue = "1500.00", reason = "Freight added per PO", actorType = "reviewer" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var package = await ReadJson(response);
        var record = package.GetProperty("documents")[0].GetProperty("records")[0];
        Assert.Equal("1450.00", record.GetProperty("rawValue").GetString());
        Assert.Equal("1500.00", record.GetProperty("currentValue").GetString());
        Assert.Equal(3, record.GetProperty("valueVersion").GetInt32());
        var last = LastProvenanceEntry(record);
        Assert.Equal("corrected", last.GetProperty("eventType").GetString());
        Assert.Equal("1495.00", last.GetProperty("previousValue").GetString());
        Assert.Equal(TestActor, last.GetProperty("actorId").GetString());
        Assert.Contains(package.GetProperty("auditEvents").EnumerateArray(), audit =>
            audit.GetProperty("eventType").GetString() == "user-correction"
            && audit.GetProperty("recordId").GetString() == recordId
            && audit.GetProperty("actorId").GetString() == TestActor);
    }

    [Fact]
    public async Task Correction_OnConfirmedEvidence_RequiresReplacement()
    {
        var created = await CreateAsync(PackageJson(NewCase()));
        var packageId = created.GetProperty("evidencePackageId").GetString()!;
        await FinalizeAsync(packageId);

        var response = await _client.PostAsJsonAsync($"{BasePath}/{packageId}/records/{FirstRecordId(created)}/corrections",
            new { newValue = "1.00", reason = "late change" });

        Assert.Equal(HttpStatusCode.PreconditionFailed, response.StatusCode);
    }

    [Fact]
    public async Task Verification_IsRecordedAsSeparateEventWithVersionInformation()
    {
        var created = await CreateAsync(PackageJson(NewCase()));
        var packageId = created.GetProperty("evidencePackageId").GetString()!;
        var recordId = FirstRecordId(created);

        var response = await _client.PostAsJsonAsync($"{BasePath}/{packageId}/records/{recordId}/verifications",
            new { verificationStatus = "passed", reason = "Arithmetic check", modelVersion = "ocr-2.1", schemaVersion = "ap-evidence-v1" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var package = await ReadJson(response);
        var record = package.GetProperty("documents")[0].GetProperty("records")[0];
        Assert.Equal("passed", record.GetProperty("verificationStatus").GetString());
        Assert.Equal("1495.00", record.GetProperty("currentValue").GetString());
        Assert.Equal("verified", LastProvenanceEntry(record).GetProperty("eventType").GetString());
        var audit = package.GetProperty("auditEvents").EnumerateArray().Last();
        Assert.Equal("verification-result", audit.GetProperty("eventType").GetString());
        Assert.Equal("ocr-2.1", audit.GetProperty("metadata").GetProperty("modelVersion").GetString());
    }

    [Fact]
    public async Task StatusChange_UpdatesPackageAndRecordStatusWithAuditTrail()
    {
        var created = await CreateAsync(PackageJson(NewCase()));
        var packageId = created.GetProperty("evidencePackageId").GetString()!;
        var recordId = FirstRecordId(created);

        var packageChange = await _client.PostAsJsonAsync($"{BasePath}/{packageId}/status", new { reviewStatus = "pending-review" });
        var recordChange = await _client.PostAsJsonAsync($"{BasePath}/{packageId}/status", new { reviewStatus = "rejected", recordId, reason = "Duplicate line" });
        var invalid = await _client.PostAsJsonAsync($"{BasePath}/{packageId}/status", new { reviewStatus = "confirmed" });

        Assert.Equal(HttpStatusCode.OK, packageChange.StatusCode);
        Assert.Equal(HttpStatusCode.OK, recordChange.StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, invalid.StatusCode);
        var package = await GetAsync(packageId);
        Assert.Equal("pending-review", package.GetProperty("reviewStatus").GetString());
        Assert.Equal("rejected", package.GetProperty("documents")[0].GetProperty("records")[0].GetProperty("reviewStatus").GetString());
        var types = EventTypes(package);
        Assert.Contains("status-changed", types);
        Assert.Contains("rejected", types);
    }

    // ---- US3: separation, match linkage, failure states ----

    [Fact]
    public async Task MatchOutcome_IsLinkedToEvidenceAndFlagsPartialSupport()
    {
        var created = await CreateAsync(PackageJson(NewCase()));
        var packageId = created.GetProperty("evidencePackageId").GetString()!;
        await FinalizeAsync(packageId);
        var recordId = FirstRecordId(created);

        var response = await _client.PostAsJsonAsync($"{BasePath}/{packageId}/match-outcomes", new
        {
            matchReviewId = "MR-77",
            outcome = "discrepancy",
            supportingRecordIds = new[] { recordId, "missing-record" },
            reason = "Invoice 1495.00 exceeds PO"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var package = await ReadJson(response);
        Assert.Equal("MR-77", package.GetProperty("relatedMatchReviewId").GetString());
        var outcome = package.GetProperty("matchOutcomes")[0];
        Assert.Equal("discrepancy", outcome.GetProperty("outcome").GetString());
        Assert.False(outcome.GetProperty("evidenceComplete").GetBoolean());
        Assert.Equal(recordId, outcome.GetProperty("supportingRecordIds")[0].GetString());
        Assert.Equal("missing-record", outcome.GetProperty("missingRecordIds")[0].GetString());
        Assert.DoesNotContain("1495.00", outcome.GetProperty("reason").GetString());

        var audit = package.GetProperty("auditEvents").EnumerateArray().Last();
        Assert.Equal("match-outcome", audit.GetProperty("eventType").GetString());
        Assert.DoesNotContain("1495.00", audit.GetRawText());
    }

    [Fact]
    public async Task MatchOutcome_OnDraftEvidence_IsRejected()
    {
        var packageId = await CreateIdAsync(PackageJson(NewCase()));

        var response = await _client.PostAsJsonAsync($"{BasePath}/{packageId}/match-outcomes",
            new { matchReviewId = "MR-1", outcome = "matched", supportingRecordIds = Array.Empty<string>() });

        Assert.Equal(HttpStatusCode.PreconditionFailed, response.StatusCode);
    }

    [Fact]
    public async Task Create_WhenPersistenceFails_ReturnsFailureAndCaseShowsFailedState()
    {
        var caseId = NewCase();
        _factory.Repository.FailNextSaves(1);

        var response = await _client.PostAsJsonAsync(BasePath, PackageJson(caseId));

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        var problem = await ReadJson(response);
        Assert.Equal("failed", problem.GetProperty("reviewStatus").GetString());
        Assert.Null(response.Headers.Location);

        var packages = await ReadJson(await _client.GetAsync($"{BasePath}?caseId={caseId}"));
        var stored = Assert.Single(packages.EnumerateArray());
        Assert.Equal("failed", stored.GetProperty("reviewStatus").GetString());
        Assert.Contains("save-failed", EventTypes(stored));
    }

    [Fact]
    public async Task Finalize_WhenPersistenceFails_LeavesPackageInPriorPendingState()
    {
        var packageId = await CreateIdAsync(PackageJson(NewCase()));
        _factory.Repository.FailNextSaves(1);

        var response = await _client.PostAsync($"{BasePath}/{packageId}/finalize", null);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("draft", (await ReadJson(response)).GetProperty("reviewStatus").GetString());
        var stored = await GetAsync(packageId);
        Assert.Equal("draft", stored.GetProperty("reviewStatus").GetString());
        Assert.False(stored.TryGetProperty("finalizedAt", out var finalizedAt) && finalizedAt.ValueKind != JsonValueKind.Null);
    }

    [Fact]
    public async Task ClientAuditEvents_HaveCredentialsAndRawValuesRedacted()
    {
        var request = PackageJson(NewCase());
        request["auditEvents"] = new JsonArray(new JsonObject
        {
            ["eventType"] = "source-attached",
            ["actorType"] = "system",
            ["message"] = "Imported 1450.00 with password=hunter2",
            ["metadata"] = new JsonObject { ["apiKey"] = "sk-123", ["note"] = "total 1495.00" }
        });

        var package = await CreateAsync(request);

        var clientEvent = package.GetProperty("auditEvents").EnumerateArray()
            .Single(audit => audit.GetProperty("actorType").GetString() == "system" && audit.GetProperty("eventType").GetString() == "source-attached");
        var text = clientEvent.GetRawText();
        Assert.DoesNotContain("1450.00", text);
        Assert.DoesNotContain("1495.00", text);
        Assert.DoesNotContain("hunter2", text);
        Assert.DoesNotContain("sk-123", text);
        Assert.Equal(SensitiveDataGuard.Redacted, clientEvent.GetProperty("metadata").GetProperty("apiKey").GetString());
    }

    [Fact]
    public async Task ClientAuditEvents_CannotForgeServerOnlyEvents()
    {
        var request = PackageJson(NewCase());
        request["auditEvents"] = new JsonArray(new JsonObject
        {
            ["eventType"] = "finalized",
            ["actorType"] = "system",
            ["message"] = "forged"
        });

        var response = await _client.PostAsJsonAsync(BasePath, request);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    // ---- Retention ----

    [Fact]
    public async Task Archive_OnlyAfterRetentionPeriod_AndRestrictsContentOnRetrieval()
    {
        var packageId = await CreateIdAsync(PackageJson(NewCase()));
        await FinalizeAsync(packageId);

        var early = await _client.PostAsync($"{BasePath}/{packageId}/archive", null);
        Assert.Equal(HttpStatusCode.PreconditionFailed, early.StatusCode);

        _factory.Clock.Advance(TimeSpan.FromDays((3 * 366) + 1));
        var archived = await _client.PostAsync($"{BasePath}/{packageId}/archive", null);
        Assert.Equal(HttpStatusCode.OK, archived.StatusCode);

        var package = await GetAsync(packageId);
        Assert.Equal("archived", package.GetProperty("reviewStatus").GetString());
        Assert.True(package.GetProperty("contentRestricted").GetBoolean());
        var document = package.GetProperty("documents")[0];
        Assert.Equal(JsonValueKind.Null, document.GetProperty("storageLocation").ValueKind);
        Assert.Equal("INV-10492", document.GetProperty("sourceReference").GetString());
        var record = document.GetProperty("records")[0];
        Assert.Equal(JsonValueKind.Null, record.GetProperty("rawValue").ValueKind);
        Assert.Equal(JsonValueKind.Null, record.GetProperty("currentValue").ValueKind);
        Assert.NotEmpty(record.GetProperty("provenance").EnumerateArray());
        Assert.Contains("archived", EventTypes(package));
    }

    [Fact]
    public async Task Archive_DraftPackage_IsRejected()
    {
        var packageId = await CreateIdAsync(PackageJson(NewCase()));

        var response = await _client.PostAsync($"{BasePath}/{packageId}/archive", null);

        Assert.Equal(HttpStatusCode.PreconditionFailed, response.StatusCode);
    }

    // ---- SC-001 / SC-003 / SC-005: lineage regression and retrieval budget ----

    [Fact]
    public async Task Retrieval_OfThreeWayPackage_StaysWithinBudgetAndPreservesSeparateLineage()
    {
        var request = PackageJson(NewCase());
        var documents = request["documents"]!.AsArray();
        documents.Add(DocumentJson("purchase-order", "PO-5521", "purchase-order-commitment", "1500.00", "1500.00"));
        documents.Add(DocumentJson("receipt", "GRN-889", "goods-receipt", "10", "10"));
        var packageId = await CreateIdAsync(request);
        await FinalizeAsync(packageId);

        var timings = new List<TimeSpan>();
        JsonElement package = default;
        for (var i = 0; i < 20; i++)
        {
            var stopwatch = Stopwatch.StartNew();
            package = await GetAsync(packageId);
            timings.Add(stopwatch.Elapsed);
        }

        var p95 = timings.OrderBy(t => t).ElementAt((int)Math.Ceiling(timings.Count * 0.95) - 1);
        Assert.True(p95 < TimeSpan.FromSeconds(10), $"p95 retrieval was {p95}.");
        Assert.Equal("mixed", package.GetProperty("packageType").GetString());

        var categories = package.GetProperty("documents").EnumerateArray()
            .Select(document => (
                Type: document.GetProperty("documentType").GetString(),
                Category: document.GetProperty("records")[0].GetProperty("recordCategory").GetString()))
            .ToList();
        Assert.Contains(("invoice", "invoice-line"), categories);
        Assert.Contains(("purchase-order", "purchase-order-commitment"), categories);
        Assert.Contains(("receipt", "goods-receipt"), categories);

        foreach (var document in package.GetProperty("documents").EnumerateArray())
        {
            var record = document.GetProperty("records")[0];
            Assert.Equal(document.GetProperty("documentId").GetString(), record.GetProperty("documentId").GetString());
            Assert.Equal("confirmed", record.GetProperty("reviewStatus").GetString());
            Assert.NotEmpty(record.GetProperty("provenance").EnumerateArray());
        }

        Assert.Equal(3, EventTypes(package).Count(type => type == "source-attached"));
    }

    // ---- Durable repository ----

    [Fact]
    public void FileRepository_PersistsAcrossInstances_AndRejectsNonGuidIds()
    {
        var root = Path.Combine(Path.GetTempPath(), $"ilp-evidence-{Guid.NewGuid():N}");
        try
        {
            var package = new EvidencePackage { CaseId = "AP-FILE-1", ReviewStatus = ReviewStatus.Confirmed };
            package.Documents.Add(new EvidenceDocument { DocumentType = DocumentType.Invoice, SourceReference = "INV-1" });
            new FileEvidenceRepository(root).Save(package);

            var reopened = new FileEvidenceRepository(root);
            var loaded = reopened.Get(package.EvidencePackageId);

            Assert.NotNull(loaded);
            Assert.Equal(ReviewStatus.Confirmed, loaded.ReviewStatus);
            Assert.Equal(DocumentType.Invoice, loaded.Documents[0].DocumentType);
            Assert.Single(reopened.List());
            Assert.Null(reopened.Get(@"..\..\secrets"));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task FileContentStore_SavesAllParts_IgnoresClientPathsAndRejectsNonGuidIds()
    {
        var root = Path.Combine(Path.GetTempPath(), $"ilp-content-{Guid.NewGuid():N}");
        try
        {
            var store = new FileDocumentContentStore(root);
            var id = Guid.NewGuid().ToString();

            var location = await store.SaveAsync(id, [new DocumentContentPart(@"..\..\page-1.jpg", [1, 2, 3], "image/jpeg")]);

            Assert.Equal($"protected://source-documents/{id}", location);
            Assert.True(await store.ExistsAsync(id));
            Assert.Equal([1, 2, 3], File.ReadAllBytes(Path.Combine(root, id, "page-1.jpg")));
            await Assert.ThrowsAsync<ArgumentException>(() => store.SaveAsync(@"..\..\secrets", []));
            Assert.False(await store.ExistsAsync(@"..\..\secrets"));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    // ---- helpers ----

    private static string NewCase() => $"AP-CASE-{Guid.NewGuid():N}";

    private static JsonObject PackageJson(string caseId, string sourceReference = "INV-10492") => new()
    {
        ["caseId"] = caseId,
        ["reviewStatus"] = "draft",
        ["documents"] = new JsonArray(InvoiceDocumentJson(sourceReference)),
        ["auditEvents"] = new JsonArray()
    };

    private static JsonObject InvoiceDocumentJson(string sourceReference)
    {
        var document = DocumentJson("invoice", sourceReference, "invoice-line", "1450.00", "1495.00");
        var record = document["records"]![0]!.AsObject();
        record["modelVersion"] = "ocr-2.0";
        record["schemaVersion"] = "ap-evidence-v1";
        record["provenance"] = new JsonArray(
            new JsonObject
            {
                ["eventType"] = "extracted",
                ["actorType"] = "model",
                ["newValue"] = "1450.00",
                ["sourceReference"] = "ocr/invoice-10492"
            },
            new JsonObject
            {
                ["eventType"] = "corrected",
                ["actorType"] = "user",
                ["previousValue"] = "1450.00",
                ["newValue"] = "1495.00",
                ["reason"] = "supplier confirmed revised amount"
            });
        return document;
    }

    private static JsonObject DocumentJson(string documentType, string sourceReference, string recordCategory, string rawValue, string currentValue) => new()
    {
        ["documentType"] = documentType,
        ["sourceReference"] = sourceReference,
        ["reviewStatus"] = "draft",
        ["storageLocation"] = $"protected://evidence/{documentType}/{sourceReference}.pdf",
        ["checksum"] = "sha256:abc123",
        ["records"] = new JsonArray(new JsonObject
        {
            ["recordCategory"] = recordCategory,
            ["recordType"] = "amount",
            ["rawValue"] = rawValue,
            ["currentValue"] = currentValue,
            ["reviewStatus"] = "draft",
            ["provenance"] = new JsonArray(new JsonObject
            {
                ["eventType"] = "extracted",
                ["actorType"] = "model",
                ["newValue"] = rawValue
            })
        })
    };

    private async Task<JsonElement> CreateAsync(JsonObject request)
    {
        var response = await _client.PostAsJsonAsync(BasePath, request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await ReadJson(response);
    }

    private async Task<string> CreateIdAsync(JsonObject request) =>
        (await CreateAsync(request)).GetProperty("evidencePackageId").GetString()!;

    private async Task<JsonElement> FinalizeAsync(string packageId)
    {
        var response = await _client.PostAsync($"{BasePath}/{packageId}/finalize", null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await ReadJson(response);
    }

    private async Task<JsonElement> GetAsync(string packageId)
    {
        var response = await _client.GetAsync($"{BasePath}/{packageId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await ReadJson(response);
    }

    private static async Task<JsonElement> ReadJson(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.Clone();

    private static string FirstRecordId(JsonElement package) =>
        package.GetProperty("documents")[0].GetProperty("records")[0].GetProperty("recordId").GetString()!;

    private static JsonElement LastProvenanceEntry(JsonElement record) =>
        record.GetProperty("provenance").EnumerateArray().Last();

    private static List<string?> EventTypes(JsonElement package) =>
        package.GetProperty("auditEvents").EnumerateArray().Select(audit => audit.GetProperty("eventType").GetString()).ToList();
}
