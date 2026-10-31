using Microsoft.Extensions.Configuration;

namespace ILP.Shared.InboxMonitoring.Config;

public static class InboxWatchConfig
{
    public const string DefaultTopicName = "projects/ethereal-yen-232614/topics/smuilpteam5-inbox";
    public const int DefaultRenewIntervalHours = 24;

    public static string m_topicName = DefaultTopicName;
    public static int m_renewIntervalHours = DefaultRenewIntervalHours;

    public static string TopicName { get { return m_topicName; } }
    public static TimeSpan RenewInterval { get { return TimeSpan.FromHours(m_renewIntervalHours); } }

    public static void Initialize(IConfiguration config)
    {
        m_topicName = config["AppSettings:InboxWatch:TopicName"] ?? "";
        if (string.IsNullOrWhiteSpace(m_topicName))
        {
            m_topicName = DefaultTopicName;
        }

        if (!int.TryParse(config["AppSettings:InboxWatch:RenewIntervalHours"], out m_renewIntervalHours)
            || m_renewIntervalHours <= 0)
        {
            m_renewIntervalHours = DefaultRenewIntervalHours;
        }
    }
}
