using ILP.Shared.TransactionDocument.Dto;

namespace ILP.Shared.InboxMonitoring.Dto;

public sealed record DocumentInboxMessageMappingDto(
    DocumentDto Document,
    InboxMessageDto InboxMessage,
    InboxMessageAttachmentDto? SourceAttachment = null);
