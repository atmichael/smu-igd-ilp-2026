using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using ILP.Shared.InboxMonitoring.Abstraction;
using ILP.Shared.InboxMonitoring.Config;
using ILP.Shared.InboxMonitoring.Dto;
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

    public async Task<InboxMessageAttachmentDto> UploadAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var credential = await AuthorizeAsync(cancellationToken);
        using var service = new DriveService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "SMU ILP"
        });

        var fileMetadata = new Google.Apis.Drive.v3.Data.File
        {
            Name = Path.GetFileName(filePath),
            Parents = string.IsNullOrWhiteSpace(_options.GoogleDriveFolderId) ? null : [_options.GoogleDriveFolderId]
        };

        await using var stream = System.IO.File.OpenRead(filePath);
        var request = service.Files.Create(fileMetadata, stream, GetContentType(filePath));
        request.Fields = "id,webViewLink";
        await request.UploadAsync(cancellationToken);

        var uploadedFile = request.ResponseBody
            ?? throw new InvalidOperationException("Google Drive did not return the uploaded file metadata.");

        return new InboxMessageAttachmentDto(
            uploadedFile.Name ?? Path.GetFileName(filePath),
            uploadedFile.Id ?? throw new InvalidOperationException("Google Drive did not return a file id."),
            uploadedFile.WebViewLink ?? $"https://drive.google.com/file/d/{uploadedFile.Id}/view");
    }

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
