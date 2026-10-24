using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using ILP.Shared.InboxMonitoring.Abstraction;
using ILP.Shared.InboxMonitoring.Config;
using ILP.Shared.InboxMonitoring.Dto;
using ILP.Shared.TransactionDocument.Dto;
using Microsoft.Extensions.Options;

namespace ILP.Shared.InboxMonitoring.Service;

public sealed class GoogleDriveFileProvider : IGoogleDriveFileProvider
{
    private static readonly string[] Scopes = [DriveService.Scope.DriveFile];

    private readonly InboxMonitoringOptions _options;

    public GoogleDriveFileProvider(IOptions<InboxMonitoringOptions> options)
    {
        _options = options.Value;
    }

    public async Task<InboxMessageAttachmentDto> UploadAsync(
        string filePath,
        DocumentDto document,
        CancellationToken cancellationToken = default)
    {
        var credential = await AuthorizeAsync(cancellationToken);
        using var service = new DriveService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "SMU ILP"
        });

        string folderId = await EnsureDocumentFolderAsync(service, document, cancellationToken);
        var fileMetadata = new Google.Apis.Drive.v3.Data.File
        {
            Name = Path.GetFileName(filePath),
            Parents = [folderId]
        };

        await using var stream = System.IO.File.OpenRead(filePath);
        var request = service.Files.Create(fileMetadata, stream, GetContentType(filePath));
        request.Fields = "id,name,webViewLink";
        await request.UploadAsync(cancellationToken);

        var uploadedFile = request.ResponseBody
            ?? throw new InvalidOperationException("Google Drive did not return the uploaded file metadata.");

        return new InboxMessageAttachmentDto(
            uploadedFile.Name ?? Path.GetFileName(filePath),
            uploadedFile.Id ?? throw new InvalidOperationException("Google Drive did not return a file id."),
            uploadedFile.WebViewLink ?? $"https://drive.google.com/file/d/{uploadedFile.Id}/view");
    }

    internal static IReadOnlyList<string> GetFolderNames(DocumentDto document) =>
    [
        NormalizeFolderName(document.Company.UEN, "Unknown UEN"),
        NormalizeFolderName(document.TypeCode, "Unknown document type"),
        NormalizeFolderName(document.RefNumber, "Unnumbered document")
    ];

    private async Task<string> EnsureDocumentFolderAsync(
        DriveService service,
        DocumentDto document,
        CancellationToken cancellationToken)
    {
        string? parentFolderId = string.IsNullOrWhiteSpace(_options.GoogleDriveFolderId)
            ? null
            : _options.GoogleDriveFolderId;

        foreach (string folderName in GetFolderNames(document))
        {
            parentFolderId = await FindOrCreateFolderAsync(service, folderName, parentFolderId, cancellationToken);
        }

        return parentFolderId;
    }

    private static async Task<string> FindOrCreateFolderAsync(
        DriveService service,
        string folderName,
        string? parentFolderId,
        CancellationToken cancellationToken)
    {
        string parentQuery = parentFolderId == null ? "'root' in parents" : $"'{EscapeQueryValue(parentFolderId)}' in parents";
        var listRequest = service.Files.List();
        listRequest.Q = $"name = '{EscapeQueryValue(folderName)}' and mimeType = 'application/vnd.google-apps.folder' and trashed = false and {parentQuery}";
        listRequest.Spaces = "drive";
        listRequest.Fields = "files(id,name)";
        listRequest.PageSize = 1;

        var matches = await listRequest.ExecuteAsync(cancellationToken);
        string? folderId = matches.Files?.FirstOrDefault()?.Id;
        if (!string.IsNullOrWhiteSpace(folderId))
        {
            return folderId;
        }

        var folder = new Google.Apis.Drive.v3.Data.File
        {
            Name = folderName,
            MimeType = "application/vnd.google-apps.folder",
            Parents = parentFolderId == null ? null : [parentFolderId]
        };
        var createRequest = service.Files.Create(folder);
        createRequest.Fields = "id";
        var createdFolder = await createRequest.ExecuteAsync(cancellationToken);
        return createdFolder.Id
            ?? throw new InvalidOperationException($"Google Drive did not return an id for folder '{folderName}'.");
    }

    private static string NormalizeFolderName(string? name, string fallback)
    {
        string value = string.IsNullOrWhiteSpace(name) ? fallback : name.Trim();
        return string.Concat(value.Select(character =>
            character is '/' or '\\' || char.IsControl(character) ? '_' : character));
    }

    private static string EscapeQueryValue(string value) =>
        value.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("'", "\\'", StringComparison.Ordinal);

    private async Task<UserCredential> AuthorizeAsync(CancellationToken cancellationToken)
    {
        using var stream = new FileStream(_options.ClientSecretPath, FileMode.Open, FileAccess.Read);
        var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
            GoogleClientSecrets.FromStream(stream).Secrets,
            Scopes,
            _options.EmailAddress,
            cancellationToken,
            new FileDataStore(Path.GetFullPath(_options.GoogleDriveTokenStorePath), true));

        if (credential.Token.IsStale)
        {
            await credential.RefreshTokenAsync(cancellationToken);
        }

        return credential;
    }

    private static string GetContentType(string filePath) =>
        Path.GetExtension(filePath).ToLowerInvariant() switch
        {
            ".pdf" => "application/pdf",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".xls" => "application/vnd.ms-excel",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            _ => "application/octet-stream"
        };
}
