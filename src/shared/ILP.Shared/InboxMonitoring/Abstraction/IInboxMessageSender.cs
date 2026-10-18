using ILP.Shared.InboxMonitoring.Dto;

namespace ILP.Shared.InboxMonitoring.Abstraction;

public interface IInboxMessageSender
{
    Task SendAttachmentsAsync(
        string recipient,
        string subject,
        string body,
        IReadOnlyList<EmailAttachmentDto> attachments,
        CancellationToken cancellationToken = default);
}
