using Google.Apis.Auth.OAuth2;
using Google.Apis.Util.Store;
using ILP.Shared.InboxMonitoring.Abstraction;
using ILP.Shared.InboxMonitoring.Config;
using ILP.Shared.InboxMonitoring.Dto;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace ILP.Shared.InboxMonitoring.Service;

public sealed class GmailInboxMessageSender : IInboxMessageSender
{
    private static readonly string[] Scopes = ["https://mail.google.com/"];

    private readonly InboxMonitoringOptions _options;

    public GmailInboxMessageSender(IOptions<InboxMonitoringOptions> options)
    {
        _options = options.Value;
    }

    public async Task SendAttachmentsAsync(
        string recipient,
        string subject,
        string body,
        IReadOnlyList<EmailAttachmentDto> attachments,
        CancellationToken cancellationToken = default)
    {
        var credential = await AuthorizeAsync(cancellationToken);
        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(_options.EmailAddress));
        message.To.Add(MailboxAddress.Parse(recipient));
        message.Subject = subject;

        var bodyBuilder = new BodyBuilder { TextBody = body };
        foreach (var attachment in attachments)
        {
            bodyBuilder.Attachments.Add(
                attachment.FileName,
                attachment.Content,
                ContentType.Parse(attachment.ContentType));
        }

        message.Body = bodyBuilder.ToMessageBody();

        using var client = new SmtpClient();
        await client.ConnectAsync("smtp.gmail.com", 587, SecureSocketOptions.StartTls, cancellationToken);
        await client.AuthenticateAsync(
            new SaslMechanismOAuth2(_options.EmailAddress, credential.Token.AccessToken),
            cancellationToken);
        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
    }

    private async Task<UserCredential> AuthorizeAsync(CancellationToken cancellationToken)
    {
        using var stream = new FileStream(_options.ClientSecretPath, FileMode.Open, FileAccess.Read);
        string credentialPath = Path.GetFullPath(_options.TokenStorePath);

        var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
            GoogleClientSecrets.FromStream(stream).Secrets,
            Scopes,
            _options.EmailAddress,
            cancellationToken,
            new FileDataStore(credentialPath, true));

        if (credential.Token.IsStale)
        {
            await credential.RefreshTokenAsync(cancellationToken);
        }

        return credential;
    }
}
