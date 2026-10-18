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

    public string ImapHost { get; set; } = "imap.gmail.com";

    public int ImapPort { get; set; } = 993;

    public TimeSpan PollInterval => TimeSpan.FromSeconds(Math.Max(1, PollIntervalSeconds));
}
