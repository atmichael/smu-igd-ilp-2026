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
                var attachment = request.Messages[0].Content[2];
                var dataUri = $"data:{mimeType};base64,{Convert.ToBase64String(attachmentBytes)}";

                if (mimeType.StartsWith("image/", StringComparison.Ordinal))
                {
                    Assert.Equal("image_url", attachment.Type);
                    Assert.Equal(dataUri, attachment.ImageUrl!.Url);
                }
                else
                {
                    Assert.Equal("file", attachment.Type);
                    Assert.Equal($"attachment{extension}", attachment.File!.FileName);
                    Assert.Equal(dataUri, attachment.File.FileData);
                }
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

        [Fact]
        public async Task GetChatRequest_IncludesInlineImageFromEmailBody()
        {
            const string imageData = "AQID";
            var emailBody = $"<p>Invoice screenshot</p><img src=\"data:image/png;base64,{imageData}\" />";

            var request = await EmailInfoExtractionProvider.GetChatRequest(emailBody, "", "");
            var image = Assert.Single(request.Messages[0].Content, content => content.Type == "image_url");

            Assert.Equal($"data:image/png;base64,{imageData}", image.ImageUrl!.Url);
        }

        [Fact]
        public async Task GetChatRequest_PreservesFormattedHtmlDocumentBodyAsReadableText()
        {
            const string emailBody = """
                <table>
                    <tr><th>Invoice No.</th><th>Total</th></tr>
                    <tr><td>INV-123</td><td>$125.00</td></tr>
                </table>
                """;

            var request = await EmailInfoExtractionProvider.GetChatRequest(emailBody, "", "");
            var bodyText = request.Messages[0].Content[1].Text!;

            Assert.Contains("Invoice No.", bodyText);
            Assert.Contains("INV-123", bodyText);
            Assert.Contains("$125.00", bodyText);
            Assert.DoesNotContain("<table>", bodyText);
        }
    }
}
