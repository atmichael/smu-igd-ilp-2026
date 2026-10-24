using Google.Apis.Auth.OAuth2;
using Google.Apis.Util.Store;
using ILP.Shared.Helper;
using ILP.Shared.InboxMonitoring.Abstraction;
using ILP.Shared.InboxMonitoring.Config;
using ILP.Shared.InboxMonitoring.Dto;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace ILP.Shared.InboxMonitoring.Service;

public sealed class ImapInboxMessageSource : IInboxMessageSource
{
    private static readonly string[] Scopes = ["https://mail.google.com/"];

    private readonly InboxMonitoringOptions _options;

    public ImapInboxMessageSource(IOptions<InboxMonitoringOptions> options)
    {
        _options = options.Value;
    }

    public async Task<IReadOnlyList<InboxMessageDto>> FetchUnreadAsync(string traceId, CancellationToken cancellationToken)
    {
        var credential = await AuthorizeAsync(cancellationToken);

        var result = new List<InboxMessageDto>();
        using var client = new ImapClient();
        await client.ConnectAsync(_options.ImapHost, _options.ImapPort, SecureSocketOptions.SslOnConnect, cancellationToken);
        await client.AuthenticateAsync(new SaslMechanismOAuth2(_options.EmailAddress, credential.Token.AccessToken), cancellationToken);

        var inbox = client.Inbox;
        await inbox.OpenAsync(FolderAccess.ReadWrite, cancellationToken);

        LogHelper.Info(traceId, $"Total messages: {inbox.Count}");
        LogHelper.Info(traceId, $"Recent messages: {inbox.Recent}");

        var uniqueIds = await inbox.SearchAsync(SearchQuery.NotSeen, cancellationToken);
        foreach (var uid in uniqueIds)
        {
            var message = await inbox.GetMessageAsync(uid, cancellationToken);
            result.Add(await ToDtoAsync(message, uid.Id, traceId, cancellationToken));
        }

        await client.DisconnectAsync(true, cancellationToken);
        return result;
    }

    public async Task MarkAsReadAsync(IReadOnlyCollection<uint> uniqueIds, string traceId, CancellationToken cancellationToken)
    {
        if (uniqueIds.Count == 0)
        {
            return;
        }

        var credential = await AuthorizeAsync(cancellationToken);

        using var client = new ImapClient();
        await client.ConnectAsync(_options.ImapHost, _options.ImapPort, SecureSocketOptions.SslOnConnect, cancellationToken);
        await client.AuthenticateAsync(new SaslMechanismOAuth2(_options.EmailAddress, credential.Token.AccessToken), cancellationToken);

        var inbox = client.Inbox;
        await inbox.OpenAsync(FolderAccess.ReadWrite, cancellationToken);

        await inbox.AddFlagsAsync(uniqueIds.Select(id => new UniqueId(id)).ToList(), MessageFlags.Seen, true, cancellationToken);
        LogHelper.Info(traceId, $"Marked {uniqueIds.Count} message(s) as read");

        await client.DisconnectAsync(true, cancellationToken);
    }

    private async Task<UserCredential> AuthorizeAsync(CancellationToken cancellationToken)
    {
        // Opens a browser for the initial login, then stores encrypted refresh tokens locally.
        using var stream = new FileStream(_options.ClientSecretPath, FileMode.Open, FileAccess.Read);
        string credPath = Path.GetFullPath(_options.TokenStorePath);

        var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
            GoogleClientSecrets.FromStream(stream).Secrets,
            Scopes,
            _options.EmailAddress,
            cancellationToken,
            new FileDataStore(credPath, true));

        if (credential.Token.IsStale)
        {
            await credential.RefreshTokenAsync(cancellationToken);
        }

        return credential;
    }

    private static async Task<InboxMessageDto> ToDtoAsync(MimeMessage message, uint uniqueId, string traceId, CancellationToken cancellationToken)
    {
        string body = !string.IsNullOrWhiteSpace(message.HtmlBody) ? message.HtmlBody : message.TextBody ?? "";
        LogHelper.Trace(traceId, $"Body Excerpt: {body}");
        LogHelper.Info(traceId, $"Email has {message.Attachments.Count()} attachment(s)");

        var inlineImages = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var imagePart in message.BodyParts.OfType<MimePart>()
            .Where(part => part.ContentType.MimeType.StartsWith("image/", StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(part.ContentId)))
        {
            if (imagePart.Content == null)
            {
                continue;
            }

            using var stream = new MemoryStream();
            await imagePart.Content.DecodeToAsync(stream, cancellationToken);
            string contentId = imagePart.ContentId!.Trim().Trim('<', '>');
            inlineImages[contentId] = $"data:{imagePart.ContentType.MimeType};base64,{Convert.ToBase64String(stream.ToArray())}";
        }

        var attachmentPaths = new List<string>();
        foreach (var attachment in message.Attachments)
        {
            if (attachment is MimePart inlinePart
                && !string.IsNullOrWhiteSpace(inlinePart.ContentId)
                && body.Contains($"cid:{inlinePart.ContentId.Trim().Trim('<', '>')}", StringComparison.OrdinalIgnoreCase)
                && inlineImages.ContainsKey(inlinePart.ContentId.Trim().Trim('<', '>')))
            {
                continue;
            }

            string extension = Path.GetExtension(attachment.ContentDisposition?.FileName ?? "");
            string tempFilePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}{extension}");

            if (attachment is MimePart mimePart)
            {
                using var stream = File.Create(tempFilePath);
                await mimePart.Content.DecodeToAsync(stream, cancellationToken);
                attachmentPaths.Add(tempFilePath);
            }
            else if (attachment is MessagePart messagePart)
            {
                using var stream = File.Create(tempFilePath);
                await messagePart.Message.WriteToAsync(stream, cancellationToken);
                attachmentPaths.Add(tempFilePath);
            }
        }

        return new InboxMessageDto
        {
            UniqueId = uniqueId,
            Subject = message.Subject ?? "",
            From = message.From.ToString(),
            Body = body,
            InlineImages = inlineImages,
            AttachmentPaths = attachmentPaths
        };
    }
}
