namespace ILP.Shared.InboxMonitoring.Abstraction;

public interface IInboxWatchClient
{
    Task WatchAsync(string topicName, CancellationToken cancellationToken = default);
}
