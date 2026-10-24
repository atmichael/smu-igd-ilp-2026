using ILP.Shared.InboxMonitoring.Dto;

namespace ILP.Shared.InboxMonitoring.Abstraction;

public interface IGoogleDriveFileProvider
{
    Task<InboxMessageAttachmentDto> UploadAsync(string filePath, CancellationToken cancellationToken = default);
}
