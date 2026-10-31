using ILP.Shared.InboxMonitoring.Dto;

namespace ILP.Shared.InboxMonitoring.Abstraction;

public interface IDocumentNotificationPublisher
{
    void Publish(DocumentNotificationDto notification);
}
