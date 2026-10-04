using ILP.Server.Features.EvidenceStorage;
using ILP.Shared.Evidence;
using Microsoft.AspNetCore.Mvc;

namespace ILP.Server.Endpoints.EvidencePackages;

public static class EvidencePackagesEndpoints
{
    public static void MapEvidencePackagesEndpoints(this IEndpointRouteBuilder app)
    {
        var store = new EvidencePackageStore();

        app.MapPost("/api/evidence-packages", async (HttpRequest request) =>
        {
            try
            {
                var payload = await request.ReadFromJsonAsync<CreateEvidencePackageRequest>();
                if (payload is null)
                {
                    return Results.BadRequest(new ProblemDetails
                    {
                        Title = "Invalid request payload.",
                        Detail = "The evidence package request body is required.",
                        Status = StatusCodes.Status400BadRequest
                    });
                }

                var package = store.Create(payload);
                return Results.Created($"/api/evidence-packages/{package.EvidencePackageId}", MapResponse(package));
            }
            catch (DuplicateEvidenceException ex)
            {
                return Results.Conflict(new ProblemDetails
                {
                    Title = "Duplicate final evidence.",
                    Detail = ex.Message,
                    Status = StatusCodes.Status409Conflict
                });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new ProblemDetails
                {
                    Title = "Invalid evidence package.",
                    Detail = ex.Message,
                    Status = StatusCodes.Status400BadRequest
                });
            }
            catch (Exception)
            {
                return Results.Problem(
                    title: "Evidence package save failed.",
                    detail: "Persistence failed and no success state should be reported.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        });

        app.MapGet("/api/evidence-packages/{evidencePackageId}", (string evidencePackageId) =>
        {
            var package = store.GetById(evidencePackageId);
            if (package is null)
            {
                return Results.NotFound(new ProblemDetails
                {
                    Title = "Evidence package not found.",
                    Detail = $"No package was found for {evidencePackageId}.",
                    Status = StatusCodes.Status404NotFound
                });
            }

            return Results.Ok(MapResponse(package));
        });

        app.MapGet("/api/evidence-packages", (string? caseId, string? documentId, string? sourceReference, string? reviewStatus) =>
        {
            var packages = store.Query(caseId, documentId, sourceReference, reviewStatus);
            return Results.Ok(packages.Select(MapResponse).ToList());
        });

        app.MapPost("/api/evidence-packages/{evidencePackageId}/finalize", (string evidencePackageId) =>
        {
            try
            {
                var package = store.Finalize(evidencePackageId);
                return Results.Ok(MapResponse(package));
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(new ProblemDetails
                {
                    Title = "Evidence package not found.",
                    Detail = ex.Message,
                    Status = StatusCodes.Status404NotFound
                });
            }
        });
    }

    private static object MapResponse(EvidencePackage package) => new
    {
        evidencePackageId = package.EvidencePackageId,
        caseId = package.CaseId,
        sourceType = package.SourceType,
        status = package.Status,
        createdAt = package.CreatedAt,
        updatedAt = package.UpdatedAt,
        retentionPolicy = package.RetentionPolicy,
        relatedMatchReviewId = package.RelatedMatchReviewId,
        documents = package.Documents.Select(document => new
        {
            sourceDocumentId = document.SourceDocumentId,
            evidencePackageId = document.EvidencePackageId,
            documentType = document.DocumentType,
            sourceReference = document.SourceReference,
            storageLocation = document.StorageLocation,
            checksum = document.Checksum,
            reviewStatus = document.ReviewStatus,
            contentHash = document.ContentHash,
            createdAt = document.CreatedAt,
            records = document.Records.Select(record => new
            {
                recordId = record.RecordId,
                sourceDocumentId = record.SourceDocumentId,
                recordCategory = record.RecordCategory,
                recordType = record.RecordType,
                rawValue = record.RawValue,
                currentValue = record.CurrentValue,
                reviewStatus = record.ReviewStatus,
                provenanceVersion = record.ProvenanceVersion,
                sourceReference = record.SourceReference,
                modelVersion = record.ModelVersion,
                schemaVersion = record.SchemaVersion,
                verificationStatus = record.VerificationStatus,
                matchedEvidenceId = record.MatchedEvidenceId,
                provenance = record.Provenance.Select(p => new
                {
                    provenanceId = p.ProvenanceId,
                    recordId = p.RecordId,
                    evidencePackageId = p.EvidencePackageId,
                    eventType = p.EventType,
                    actorType = p.ActorType,
                    actorId = p.ActorId,
                    previousValue = p.PreviousValue,
                    newValue = p.NewValue,
                    sourceReference = p.SourceReference,
                    timestamp = p.Timestamp,
                    reason = p.Reason
                }).ToList()
            }).ToList()
        }).ToList(),
        auditEvents = package.AuditEvents.Select(auditEvent => new
        {
            auditEventId = auditEvent.AuditEventId,
            evidencePackageId = auditEvent.EvidencePackageId,
            recordId = auditEvent.RecordId,
            eventType = auditEvent.EventType,
            actorType = auditEvent.ActorType,
            actorId = auditEvent.ActorId,
            message = auditEvent.Message,
            metadata = auditEvent.Metadata,
            timestamp = auditEvent.Timestamp
        }).ToList()
    };
}
