using ILP.Server.Config;
using ILP.Shared.InfoExtraction.Provider;
using ILP.Shared.Test.InfoExtraction.Config;

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
        public async Task GetChatRequest_ResolvesInlineImageContentId()
        {
            const string imageData = "AQID";
            const string contentId = "image_content_id_12345";
            var emailBody = $"<p>Invoice screenshot</p><img src=\"cid:{contentId}\" />";
            var inlineImages = new Dictionary<string, string>
            {
                [$"<{contentId}>"] = $"data:image/png;base64,{imageData}"
            };

            var request = await EmailInfoExtractionProvider.GetChatRequest(emailBody, "", "", inlineImages);
            var image = Assert.Single(request.Messages[0].Content, content => content.Type == "image_url");

            Assert.Equal($"data:image/png;base64,{imageData}", image.ImageUrl!.Url);
        }

        [Fact]
        public void HasExtractableInput_ReturnsFalseForEmptyOrMarkupOnlyBody()
        {
            Assert.False(EmailInfoExtractionProvider.HasExtractableInput("", ""));
            Assert.False(EmailInfoExtractionProvider.HasExtractableInput("<p></p><img src=\"cid:missing\" />", ""));
            Assert.False(EmailInfoExtractionProvider.HasExtractableInput("<p>Please see attached.</p>", ""));
        }

        [Fact]
        public async Task HasExtractableInput_ReturnsTrueForBodyTextOrSupportedAttachment()
        {
            Assert.True(EmailInfoExtractionProvider.HasExtractableInput("<p>Invoice INV-123, Total $125.00</p>", ""));
            Assert.True(
                EmailInfoExtractionProvider.HasExtractableInput(
                    "<img src=\"cid:image_content_id_12345\" />",
                    "",
                    new Dictionary<string, string> { ["image_content_id_12345"] = "data:image/png;base64,AQID" }));

            var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(directory);
            var attachmentPath = Path.Combine(directory, "invoice.pdf");

            try
            {
                await File.WriteAllBytesAsync(attachmentPath, [1, 2, 3], TestContext.Current.CancellationToken);
                Assert.True(EmailInfoExtractionProvider.HasExtractableInput("", attachmentPath));
            }
            finally
            {
                Directory.Delete(directory, recursive: true);
            }
        }

        [Theory]
        [InlineData("")]
        [InlineData("<p>Please see attached.</p>")]
        public async Task GetDocumentContent_ReturnsEmptyWithoutAnExtractableDocument(string emailBody)
        {
            var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(directory);
            var contentPath = Path.Combine(directory, "content.md");
            File.WriteAllText(contentPath, "initial prompt");
            PromptConfig.Initialize(new TestConfiguration(new Dictionary<string, string?>
            {
                ["Prompts:ExtractDocumentContent"] = Path.GetRelativePath(AppContext.BaseDirectory, contentPath)
            }));
            var result = await EmailInfoExtractionProvider.GetDocumentContent(emailBody);

            Assert.Empty(result);
        }

        [Fact]
        public async Task GetChatRequest_IncludesInlineImageAndDocumentAttachment()
        {
            const string imageData = "AQID";
            var emailBody = $"<img src=\"data:image/png;base64,{imageData}\" />";
            var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(directory);

            try
            {
                var attachmentPath = Path.Combine(directory, "invoice.pdf");
                var attachmentBytes = new byte[] { 4, 5, 6 };
                await File.WriteAllBytesAsync(attachmentPath, attachmentBytes, TestContext.Current.CancellationToken);

                var request = await EmailInfoExtractionProvider.GetChatRequest(emailBody, attachmentPath, "");
                var content = request.Messages[0].Content;
                var image = Assert.Single(content, item => item.Type == "image_url");
                var attachment = Assert.Single(content, item => item.Type == "file");

                Assert.Equal($"data:image/png;base64,{imageData}", image.ImageUrl!.Url);
                Assert.Equal("invoice.pdf", attachment.File!.FileName);
                Assert.Equal($"data:application/pdf;base64,{Convert.ToBase64String(attachmentBytes)}", attachment.File.FileData);
            }
            finally
            {
                Directory.Delete(directory, recursive: true);
            }
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
