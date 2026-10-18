namespace ILP.Shared.InboxMonitoring.Dto;

public sealed record EmailAttachmentDto(string FileName, byte[] Content, string ContentType);
