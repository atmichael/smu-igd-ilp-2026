using Microsoft.Extensions.Configuration;

namespace ILP.Shared.InboxMonitoring.Config;

public sealed class InboxMonitoringOptions
{
    public const string SectionName = "InboxMonitoring";

    public bool Enabled { get; set; } = true;

    public int PollIntervalSeconds { get; set; } = 300;

    public string EmailAddress { get; set; } = "";

    public bool SendSourceDocumentsToEmail { get; set; }

    public string ClientSecretPath { get; set; } = "";

    public string TokenStorePath { get; set; } = "token_store";

    public string GoogleDriveFolderId { get; set; } = "";

    public string GoogleDriveTokenStorePath { get; set; } = "google_drive_token_store";

    public string ImapHost { get; set; } = "imap.gmail.com";

    public int ImapPort { get; set; } = 993;

    /// <summary>Raw "AutoMarkAsRead" setting; "Y" (case-insensitive) or unset means true, anything else false.</summary>
    [ConfigurationKeyName("AutoMarkAsRead")]
    public string? AutoMarkAsReadSetting { get; set; }

    [ConfigurationKeyName("AutoMarkAsReadResolved")]
    public bool AutoMarkAsRead =>
        string.IsNullOrWhiteSpace(AutoMarkAsReadSetting)
        || string.Equals(AutoMarkAsReadSetting.Trim(), "Y", StringComparison.OrdinalIgnoreCase);

    public TimeSpan PollInterval => TimeSpan.FromSeconds(Math.Max(1, PollIntervalSeconds));
}
