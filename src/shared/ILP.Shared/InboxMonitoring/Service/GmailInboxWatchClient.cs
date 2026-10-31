using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Util.Store;
using ILP.Shared.InboxMonitoring.Abstraction;
using ILP.Shared.InboxMonitoring.Config;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ILP.Shared.InboxMonitoring.Service;

public sealed class GmailInboxWatchClient : IInboxWatchClient
{
    private static readonly string[] Scopes = ["https://mail.google.com/"];
    private static readonly HttpClient Http = new();

    private readonly InboxMonitoringOptions _options;
    private readonly ILogger<GmailInboxWatchClient> _logger;

    public GmailInboxWatchClient(IOptions<InboxMonitoringOptions> options, ILogger<GmailInboxWatchClient> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task WatchAsync(string topicName, CancellationToken cancellationToken = default)
    {
        var credential = await AuthorizeAsync(cancellationToken);
        var body = JsonSerializer.Serialize(new
        {
            topicName,
            labelIds = new[] { "INBOX" },
            labelFilterAction = "include"
        });

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "https://gmail.googleapis.com/gmail/v1/users/me/watch")
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", credential.Token.AccessToken);

        using var response = await Http.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        _logger.LogInformation("Gmail watch activated successfully for topic {TopicName}", topicName);
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
