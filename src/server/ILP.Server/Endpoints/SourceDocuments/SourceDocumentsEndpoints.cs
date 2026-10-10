using System.Security.Cryptography;
using System.Text;
using ILP.Server.Features.EvidenceStorage;
using ILP.Shared.SourceDocuments;
using Microsoft.AspNetCore.Mvc;
using UglyToad.PdfPig;

namespace ILP.Server.Endpoints.SourceDocuments;

public static class SourceDocumentsEndpoints
{
    private const string JpegMediaType = "image/jpeg";
    private const string PngMediaType = "image/png";
    private const string PdfMediaType = "application/pdf";

    private static readonly Dictionary<string, string> FileExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        [JpegMediaType] = ".jpg",
        [PngMediaType] = ".png",
        [PdfMediaType] = ".pdf",
    };

    // Serializes the idempotency check and save so concurrent retries cannot create two documents.
    private static readonly SemaphoreSlim IntakeGate = new(1, 1);

    private sealed record IntakeContent(string MediaType, int PageCount, IReadOnlyList<DocumentContentPart> Parts);

    public static void MapEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/source-documents/{sourceDocumentId}", (string sourceDocumentId, ISourceDocumentRepository records) =>
            records.Get(sourceDocumentId) is { } record ? Results.Ok(record.Document) : Results.NotFound())
            .RequireAuthorization("SourceDocumentIntakePolicy");

        // Image pages only; page images for PDFs are derived later (Feature 04), so a PDF is read through /file.
        app.MapGet("/api/source-documents/{sourceDocumentId}/pages/{pageNumber:int}",
            async (string sourceDocumentId, int pageNumber, ISourceDocumentRepository records, IDocumentContentStore contentStore, CancellationToken cancellationToken) =>
            {
                if (records.Get(sourceDocumentId) is not { } record
                    || IsPdf(record.Document.MediaType)
                    || pageNumber < 1
                    || pageNumber > record.Document.PageCount)
                {
                    return Results.NotFound();
                }

                var content = await contentStore.ReadAsync(
                    record.Document.SourceDocumentId, PageFileName(pageNumber, record.Document.MediaType), cancellationToken);
                return content is null ? Results.NotFound() : Results.File(content, record.Document.MediaType);
            })
            .RequireAuthorization("SourceDocumentIntakePolicy");

        // The original single file of a file-upload document; camera captures are read page by page.
        app.MapGet("/api/source-documents/{sourceDocumentId}/file",
            async (string sourceDocumentId, ISourceDocumentRepository records, IDocumentContentStore contentStore, CancellationToken cancellationToken) =>
            {
                if (records.Get(sourceDocumentId) is not { } record
                    || string.Equals(record.Document.Channel, SourceDocumentChannels.CameraCapture, StringComparison.Ordinal))
                {
                    return Results.NotFound();
                }

                var content = await contentStore.ReadAsync(
                    record.Document.SourceDocumentId, SingleFileName(record.Document.MediaType), cancellationToken);
                return content is null ? Results.NotFound() : Results.File(content, record.Document.MediaType);
            })
            .RequireAuthorization("SourceDocumentIntakePolicy");

        app.MapPost("/api/source-documents", async (HttpRequest request, IDocumentContentStore contentStore, ISourceDocumentRepository records, ILoggerFactory loggerFactory) =>
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
            var idempotencyKey = request.Headers["Idempotency-Key"].FirstOrDefault();

            if (string.IsNullOrWhiteSpace(idempotencyKey))
            {
                return BadRequest("Missing idempotency key.", "The submission must include a valid Idempotency-Key header.");
            }

            // mailbox is a planned channel (Feature 03) and is rejected until implemented.
            var (intake, problem) = channel switch
            {
                SourceDocumentChannels.CameraCapture => await ReadCameraCaptureAsync(form),
                SourceDocumentChannels.FileUpload => await ReadFileUploadAsync(form),
                _ => (null, BadRequest("Invalid channel value.", "The channel must be camera-capture or file-upload.")),
            };
            if (problem is not null)
            {
                return problem;
            }

            var pageHashes = ComputePageHashes(intake!.Parts);
            var payloadHash = channel + pageHashes;

            await IntakeGate.WaitAsync(request.HttpContext.RequestAborted);
            try
            {
                if (records.FindByIdempotencyKey(idempotencyKey) is { } existing)
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
                    MediaType = intake.MediaType,
                    PageCount = intake.PageCount,
                    ContentHash = $"sha256:{Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(pageHashes))).ToLowerInvariant()}",
                };

                try
                {
                    // Not cancelled with the request, so a started save either completes or rolls back.
                    metadata.StorageLocation = await contentStore.SaveAsync(metadata.SourceDocumentId, intake.Parts, CancellationToken.None);
                    records.Save(new SourceDocumentPersistenceRecord(metadata, idempotencyKey, payloadHash));
                }
                catch (Exception ex)
                {
                    logger.LogError("Source document {SourceDocumentId} could not be stored ({ExceptionType}).", metadata.SourceDocumentId, ex.GetType().Name);
                    return Results.Problem(
                        title: "Source document could not be stored.",
                        detail: "No source document was created. Retry the submission with the same Idempotency-Key.",
                        statusCode: StatusCodes.Status500InternalServerError);
                }

                logger.LogInformation("Accepted {Channel} source document {SourceDocumentId} ({MediaType}, {PageCount} pages).",
                    metadata.Channel, metadata.SourceDocumentId, metadata.MediaType, metadata.PageCount);

                return Results.Created($"/api/source-documents/{metadata.SourceDocumentId}", metadata);
            }
            finally
            {
                IntakeGate.Release();
            }
        }).RequireAuthorization("SourceDocumentIntakePolicy");
    }

    private static async Task<(IntakeContent?, IResult?)> ReadCameraCaptureAsync(IFormCollection form)
    {
        var files = form.Files.GetFiles("pages");
        if (files.Count is < 1 or > 3)
        {
            return (null, BadRequest("Invalid page count.", "A camera-capture source document must include one to three JPEG pages."));
        }

        var parts = new List<DocumentContentPart>(files.Count);
        foreach (var file in files)
        {
            if (!string.Equals(file.ContentType, JpegMediaType, StringComparison.OrdinalIgnoreCase))
            {
                return (null, UnsupportedMediaType("Unsupported media type.", "Each page must be a JPEG image."));
            }

            var content = await ReadBytesAsync(file);
            if (DetectMediaType(content) != JpegMediaType)
            {
                return (null, UnsupportedMediaType("Invalid image format.", "Each page must contain a valid JPEG signature."));
            }

            parts.Add(new DocumentContentPart(PageFileName(parts.Count + 1, JpegMediaType), content, JpegMediaType));
        }

        return (new IntakeContent(JpegMediaType, parts.Count, parts), null);
    }

    private static async Task<(IntakeContent?, IResult?)> ReadFileUploadAsync(IFormCollection form)
    {
        var files = form.Files.GetFiles("file");
        if (files.Count != 1)
        {
            return (null, BadRequest("Invalid file count.", "A file-upload source document must include exactly one file."));
        }

        var declared = files[0].ContentType;
        if (string.IsNullOrEmpty(declared) || !FileExtensions.ContainsKey(declared))
        {
            return (null, UnsupportedMediaType("Unsupported media type.", "The file must be a PDF, JPEG, or PNG."));
        }

        var content = await ReadBytesAsync(files[0]);
        var mediaType = DetectMediaType(content);
        if (!string.Equals(mediaType, declared, StringComparison.OrdinalIgnoreCase))
        {
            return (null, UnsupportedMediaType("Invalid file content.", "The file content does not match its declared type."));
        }

        var pageCount = mediaType == PdfMediaType ? CountPdfPages(content) : 1;
        if (pageCount < 1)
        {
            return (null, UnsupportedMediaType("Unreadable PDF.", "The PDF could not be read. Upload an unencrypted, undamaged PDF."));
        }

        return (new IntakeContent(mediaType!, pageCount, [new DocumentContentPart(SingleFileName(mediaType!), content, mediaType!)]), null);
    }

    private static string? DetectMediaType(ReadOnlySpan<byte> content) =>
        content.StartsWith((ReadOnlySpan<byte>)[0xFF, 0xD8, 0xFF]) ? JpegMediaType
        : content.StartsWith((ReadOnlySpan<byte>)[0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]) ? PngMediaType
        : content.StartsWith("%PDF-"u8) ? PdfMediaType
        : null;

    private static int CountPdfPages(byte[] content)
    {
        try
        {
            using var document = PdfDocument.Open(content);
            return document.NumberOfPages;
        }
        catch (Exception)
        {
            // PdfPig throws several exception types for damaged or password-protected files.
            return 0;
        }
    }

    private static async Task<byte[]> ReadBytesAsync(IFormFile file)
    {
        using var buffer = new MemoryStream();
        await file.CopyToAsync(buffer);
        return buffer.ToArray();
    }

    private static bool IsPdf(string mediaType) => string.Equals(mediaType, PdfMediaType, StringComparison.OrdinalIgnoreCase);

    // Stored names are server-generated; client filenames are never used.
    private static string PageFileName(int pageNumber, string mediaType) => $"page-{pageNumber}{FileExtensions[mediaType]}";

    private static string SingleFileName(string mediaType) => IsPdf(mediaType) ? "original.pdf" : PageFileName(1, mediaType);

    private static IResult BadRequest(string title, string detail) =>
        Results.BadRequest(new ProblemDetails { Title = title, Detail = detail, Status = StatusCodes.Status400BadRequest });

    private static IResult UnsupportedMediaType(string title, string detail) =>
        Results.Problem(title: title, detail: detail, statusCode: StatusCodes.Status415UnsupportedMediaType);

    private static string ComputePageHashes(IReadOnlyList<DocumentContentPart> parts)
    {
        var builder = new StringBuilder();
        foreach (var part in parts)
        {
            builder.Append(Convert.ToHexString(SHA256.HashData(part.Content)));
        }

        return builder.ToString();
    }
}
