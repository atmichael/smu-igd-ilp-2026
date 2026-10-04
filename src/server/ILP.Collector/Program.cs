using Google.Apis.Auth.OAuth2;
using Google.Apis.Util.Store;
using ILP.Server.Config;
using ILP.Shared.Helper;
using ILP.Shared.InfoExtraction.Provider;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MailKit.Security;
using Microsoft.Extensions.Hosting;
using MimeKit;
using Serilog;

// Create app builder 
var builder = Host.CreateApplicationBuilder();

// Initialize OpenRouterConfig Class
OpenRouterConfig.Initialize(builder.Configuration);

// Initialize logger
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console()
    .CreateLogger();


// Application program started

string emailAddress = "smu.ilp.team5@gmail.com";

// The scope required for complete IMAP/SMTP protocol access
string[] scopes = new[] { "https://mail.google.com/" };

UserCredential credential;

// 1. Authorize via Google's Web Authorization Broker
// This opens a browser window for initial login, then stores encrypted refresh tokens locally.
using (var stream = new FileStream(@"C:\code\smu-igd-ilp-2026\deploy\secrets\client_secret_gmail.key", FileMode.Open, FileAccess.Read))
{
    string credPath = Path.Combine(Environment.CurrentDirectory, "token_store");

    credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
        GoogleClientSecrets.FromStream(stream).Secrets,
        scopes,
        emailAddress,
        CancellationToken.None,
        new FileDataStore(credPath, true)
    );
}

// 2. Ensure the access token hasn't expired (refresh it if it has)
if (credential.Token.IsStale)
{
    await credential.RefreshTokenAsync(CancellationToken.None);
}

try
{
    string traceId = Guid.NewGuid().ToString();
    // 3. Connect to Gmail using MailKit and XOAUTH2
    using (var client = new ImapClient())
    {
        await client.ConnectAsync("imap.gmail.com", 993, SecureSocketOptions.SslOnConnect);

        // Construct the SASL OAuth2 mechanism using the retrieved token
        var oauth2 = new SaslMechanismOAuth2(emailAddress, credential.Token.AccessToken);
        await client.AuthenticateAsync(oauth2);

        // 4. Perform your standard IMAP operations
        var inbox = client.Inbox;
        await inbox.OpenAsync(FolderAccess.ReadWrite);

        LogHelper.Info(traceId, $"Total messages: {inbox.Count}");
        LogHelper.Info(traceId, $"Recent messages: {inbox.Recent}");

        // 5. Search for unread messages
        var query = SearchQuery.NotSeen;
        var uniqueIds = await inbox.SearchAsync(query);

        foreach (var uid in uniqueIds)
        {
            // Fetch the full message content by its Unique ID (UID)
            MimeMessage message = await inbox.GetMessageAsync(uid);

            LogHelper.Info(traceId, $"Subject: {message.Subject}");
            LogHelper.Info(traceId, $"From: {message.From}");

            // Read the body text (handles HTML or plain text)
            string body = message.TextBody ?? message.HtmlBody;
            LogHelper.Trace(traceId, $"Body Excerpt: {body}");

            LogHelper.Info(traceId, $"Email has {message.Attachments.Count()} attachment(s)");

            var attachmentPathsList = new List<string>();

            foreach (var attachment in message.Attachments)
            {
                // Generate a temporary file path with the original extension if available
                string extension = Path.GetExtension(attachment.ContentDisposition?.FileName ?? "");
                string tempFilePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}{extension}");

                if (attachment is MimePart mimePart)
                {
                    using var stream = File.Create(tempFilePath);
                    await mimePart.Content.DecodeToAsync(stream);
                    attachmentPathsList.Add(tempFilePath);
                }
                else if (attachment is MessagePart messagePart)
                {
                    using var stream = File.Create(tempFilePath);
                    await messagePart.Message.WriteToAsync(stream);
                    attachmentPathsList.Add(tempFilePath);
                }
            }

            var attachmentPaths = attachmentPathsList;
            foreach (var file in attachmentPaths)
            {
                string extractedText = await EmailInfoExtractionProvider.GetDocumentContent(body, file, traceId);

                LogHelper.Info(traceId, "--- Extracted Text from PDF Natively ---");
                LogHelper.Info(traceId, $"Response: {extractedText}");

                // Optional: Mark the message as read (Seen)
                //await inbox.AddFlagsAsync(uid, MessageFlags.Seen, silent: true);
            }
        }

        // 6. Gracefully disconnect
        await client.DisconnectAsync(true);
    }
}
catch (Exception ex)
{
    Console.WriteLine($"An error occurred: {ex.Message}");
}
