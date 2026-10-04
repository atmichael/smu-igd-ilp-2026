using ILP.Shared.InfoExtraction.Provider;

namespace ILP.Shared.Test.InfoExtraction.Provider
{
    public class EmailInfoExtractionProviderTest
    {
        [Theory]
        [InlineData(".pdf", "application/pdf")]
        [InlineData(".png", "image/png")]
        [InlineData(".jpg", "image/jpeg")]
        [InlineData(".jpeg", "image/jpeg")]
        [InlineData(".doc", "application/msword")]
        [InlineData(".docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document")]
        [InlineData(".xls", "application/vnd.ms-excel")]
        [InlineData(".xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
        public async Task GetChatRequest_UsesMimeTypeForAttachmentExtension(string extension, string mimeType)
        {
            var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(directory);

            try
            {
                var attachmentPath = Path.Combine(directory, $"attachment{extension}");
                var attachmentBytes = new byte[] { 1, 2, 3 };
                await File.WriteAllBytesAsync(attachmentPath, attachmentBytes, TestContext.Current.CancellationToken);

                var request = await EmailInfoExtractionProvider.GetChatRequest("", attachmentPath, "");
                var attachment = request.Messages[0].Content[2].File!;

                Assert.Equal($"attachment{extension}", attachment.FileName);
                Assert.Equal(
                    $"data:{mimeType};base64,{Convert.ToBase64String(attachmentBytes)}",
                    attachment.FileData);
            }
            finally
            {
                Directory.Delete(directory, recursive: true);
            }
        }

        [Fact]
        public async Task GetChatRequest_DoesNotAttachUnsupportedFileTypes()
        {
            var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(directory);

            try
            {
                var attachmentPath = Path.Combine(directory, "attachment.txt");
                await File.WriteAllTextAsync(attachmentPath, "unsupported", TestContext.Current.CancellationToken);

                var request = await EmailInfoExtractionProvider.GetChatRequest("", attachmentPath, "");

                Assert.Equal(2, request.Messages[0].Content.Count);
            }
            finally
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }
}
