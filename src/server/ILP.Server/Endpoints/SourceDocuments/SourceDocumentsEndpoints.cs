using System.Collections.Concurrent;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
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
        app.MapPost("/api/source-documents", async (HttpRequest request, ILoggerFactory loggerFactory) =>
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
            var source = form["source"].ToString();
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

            if (!string.Equals(source, "camera-capture", StringComparison.Ordinal))
            {
                return Results.BadRequest(new ProblemDetails
                {
                    Title = "Invalid source value.",
                    Detail = "The source must be camera-capture.",
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

            var payloadHash = ComputePayloadHash(source, files);
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
                Source = source,
                PageCount = files.Count,
                Status = "received",
            };

            var entry = new SourceDocumentEntry(metadata, payloadHash);
            IdempotencyCache[idempotencyKey] = entry;

            logger.LogInformation("Accepted camera capture source document {SourceDocumentId} with {PageCount} pages.", metadata.SourceDocumentId, metadata.PageCount);

            return Results.Created($"/api/source-documents/{metadata.SourceDocumentId}", metadata);
        }).RequireAuthorization("SourceDocumentIntakePolicy");
    }

    private static string ComputePayloadHash(string source, IReadOnlyList<IFormFile> files)
    {
        using var sha = SHA256.Create();
        var builder = new StringBuilder();
        builder.Append(source);

        foreach (var file in files)
        {
            using var stream = file.OpenReadStream();
            var bytes = new byte[file.Length];
            var read = 0;

            while (read < bytes.Length)
            {
                var count = stream.Read(bytes, read, bytes.Length - read);
                if (count == 0)
                {
                    break;
                }
                read += count;
            }

            builder.Append(Convert.ToHexString(sha.ComputeHash(bytes)));
        }

        return builder.ToString();
    }

    private sealed record SourceDocumentEntry(SourceDocumentMetadata Document, string PayloadHash);
}
