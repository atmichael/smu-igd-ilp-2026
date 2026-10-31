using System.Collections.Concurrent;
using System.Threading.Channels;
using ILP.Shared.InboxMonitoring.Abstraction;
using ILP.Shared.InboxMonitoring.Dto;

namespace ILP.Server.Features.InboxMonitoring.Service;

/// <summary>Fans new-document notifications out to currently connected clients only.</summary>
public sealed class DocumentNotificationBroker : IDocumentNotificationPublisher
{
    private readonly ConcurrentDictionary<Guid, Channel<DocumentNotificationDto>> _subscribers = new();

    public int SubscriberCount => _subscribers.Count;

    public void Publish(DocumentNotificationDto notification)
    {
        foreach (var subscriber in _subscribers.Values)
        {
            subscriber.Writer.TryWrite(notification);
        }
    }

    /// <summary>Registers immediately so nothing published after this call is missed; dispose to unsubscribe.</summary>
    public Subscription Subscribe()
    {
        var id = Guid.NewGuid();
        var channel = Channel.CreateBounded<DocumentNotificationDto>(
            new BoundedChannelOptions(100) { FullMode = BoundedChannelFullMode.DropOldest });
        _subscribers[id] = channel;
        return new Subscription(channel.Reader, () => _subscribers.TryRemove(id, out _));
    }

    public sealed class Subscription(ChannelReader<DocumentNotificationDto> reader, Action onDispose) : IDisposable
    {
        public IAsyncEnumerable<DocumentNotificationDto> ReadAllAsync(CancellationToken cancellationToken) =>
            reader.ReadAllAsync(cancellationToken);

        public void Dispose() => onDispose();
    }
}
