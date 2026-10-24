using ILP.Shared.InboxMonitoring.Dto;
using ILP.Shared.TransactionDocument.Dto;

namespace ILP.Shared.InboxMonitoring.Abstraction;

public interface IGoogleDriveFileProvider
{
    Task<InboxMessageAttachmentDto> UploadAsync(
        string filePath,
        DocumentDto document,
        CancellationToken cancellationToken = default);
}
