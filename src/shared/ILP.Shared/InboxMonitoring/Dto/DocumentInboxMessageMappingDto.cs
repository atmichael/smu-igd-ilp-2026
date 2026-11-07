using ILP.Shared.TransactionDocument.Dto;
using ILP.Shared.AccountsPayable.Dto;

namespace ILP.Shared.InboxMonitoring.Dto;

public sealed record DocumentInboxMessageMappingDto(
    DocumentDto Document,
    InboxMessageDto InboxMessage,
    InboxMessageAttachmentDto? SourceAttachment = null,
    DocumentAPJournalDto? ApJournal = null);
