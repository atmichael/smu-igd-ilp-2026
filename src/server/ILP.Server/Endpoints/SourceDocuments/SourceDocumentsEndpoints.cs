using System.Collections.Concurrent;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using ILP.Server.Features.EvidenceStorage;
using ILP.Shared.SourceDocuments;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace ILP.Server.Endpoints.SourceDocuments;

public static class SourceDocumentsEndpoints
{
    private static readonly ConcurrentDictionary<string, SourceDocumentEntry> IdempotencyCache = new();

    public static void MapEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/source-documents", async (HttpRequest request, IDocumentContentStore contentStore, ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("SourceDocumentIntake");
            if (!request.HasFormContentType)
            {
                return Results.BadRequest(new ProblemDetails
                {
                    Title = "Invalid request format.",
                    Detail = "The source-document request must be multipart/form-data.",
                    Status = StatusCodes.Status400BadRequest,
                });
            }

            var form = await request.ReadFormAsync();
            var channel = form["channel"].ToString();
            var files = form.Files.GetFiles("pages").ToList();
            var idempotencyKey = request.Headers["Idempotency-Key"].FirstOrDefault();

            if (string.IsNullOrWhiteSpace(idempotencyKey))
            {
                return Results.BadRequest(new ProblemDetails
                {
                    Title = "Missing idempotency key.",
                    Detail = "The submission must include a valid Idempotency-Key header.",
                    Status = StatusCodes.Status400BadRequest,
                });
            }

            // file-upload and mailbox are planned channels (Features 01 and 03) and are rejected until implemented.
            if (!string.Equals(channel, SourceDocumentChannels.CameraCapture, StringComparison.Ordinal))
            {
                return Results.BadRequest(new ProblemDetails
                {
                    Title = "Invalid channel value.",
                    Detail = "The channel must be camera-capture.",
                    Status = StatusCodes.Status400BadRequest,
                });
            }

            if (files.Count is < 1 or > 3)
            {
                return Results.BadRequest(new ProblemDetails
                {
                    Title = "Invalid page count.",
                    Detail = "A camera-capture source document must include one to three JPEG pages.",
                    Status = StatusCodes.Status400BadRequest,
                });
            }

            foreach (var file in files)
            {
                if (!string.Equals(file.ContentType, "image/jpeg", StringComparison.OrdinalIgnoreCase))
                {
                    return Results.UnprocessableEntity(new ProblemDetails
                    {
                        Title = "Unsupported media type.",
                        Detail = "Each page must be a JPEG image.",
                        Status = StatusCodes.Status415UnsupportedMediaType,
                    });
                }

                await using var stream = file.OpenReadStream();
                var headerBytes = new byte[3];
                var read = await stream.ReadAsync(headerBytes.AsMemory(0, headerBytes.Length));
                if (read < 3 || headerBytes[0] != 0xFF || headerBytes[1] != 0xD8 || headerBytes[2] != 0xFF)
                {
                    return Results.UnprocessableEntity(new ProblemDetails
                    {
                        Title = "Invalid image format.",
                        Detail = "Each page must contain a valid JPEG signature.",
                        Status = StatusCodes.Status415UnsupportedMediaType,
                    });
                }
            }

            var pages = new List<byte[]>(files.Count);
            foreach (var file in files)
            {
                using var buffer = new MemoryStream();
                await file.CopyToAsync(buffer);
                pages.Add(buffer.ToArray());
            }

            var pageHashes = ComputePageHashes(pages);
            var payloadHash = channel + pageHashes;
            if (IdempotencyCache.TryGetValue(idempotencyKey, out var existing))
            {
                if (string.Equals(existing.PayloadHash, payloadHash, StringComparison.Ordinal))
                {
                    return Results.Created($"/api/source-documents/{existing.Document.SourceDocumentId}", existing.Document);
                }

                return Results.Conflict(new ProblemDetails
                {
                    Title = "Idempotency key conflict.",
                    Detail = "The same idempotency key was reused for different payload content.",
                    Status = StatusCodes.Status409Conflict,
                });
            }

            var metadata = new SourceDocumentMetadata
            {
                Channel = channel,
                Status = "received",
                ReceivedAt = DateTimeOffset.UtcNow,
                SubmittedBy = request.HttpContext.User.Identity?.Name,
                MediaType = "image/jpeg",
                PageCount = files.Count,
                ContentHash = $"sha256:{Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(pageHashes))).ToLowerInvariant()}",
            };

            try
            {
                metadata.StorageLocation = contentStore.Save(
                    metadata.SourceDocumentId,
                    pages.Select((bytes, index) => new DocumentContentPart($"page-{index + 1}.jpg", bytes)).ToList());
            }
            catch (Exception ex)
            {
                logger.LogError("Source document {SourceDocumentId} could not be stored ({ExceptionType}).", metadata.SourceDocumentId, ex.GetType().Name);
                return Results.Problem(
                    title: "Source document could not be stored.",
                    detail: "No source document was created. Retry the submission with the same Idempotency-Key.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }

            var entry = new SourceDocumentEntry(metadata, payloadHash);
            IdempotencyCache[idempotencyKey] = entry;

            logger.LogInformation("Accepted camera capture source document {SourceDocumentId} with {PageCount} pages.", metadata.SourceDocumentId, metadata.PageCount);

            return Results.Created($"/api/source-documents/{metadata.SourceDocumentId}", metadata);
        }).RequireAuthorization("SourceDocumentIntakePolicy");
    }

    private static string ComputePageHashes(IReadOnlyList<byte[]> pages)
    {
        var builder = new StringBuilder();
        foreach (var page in pages)
        {
            builder.Append(Convert.ToHexString(SHA256.HashData(page)));
        }

        return builder.ToString();
    }

    private sealed record SourceDocumentEntry(SourceDocumentMetadata Document, string PayloadHash);
}
