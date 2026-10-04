using ILP.Server.Config;

namespace ILP.Shared.Test.InfoExtraction.Config;

public partial class PromptConfigTest
{
    [Fact]
    public void PromptProperties_ReadUpdatedFileContentWithoutReinitializing()
    {
        var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(directory);

        try
        {
            var contentPath = Path.Combine(directory, "content.md");
            File.WriteAllText(contentPath, "initial prompt");

            PromptConfig.Initialize(new TestConfiguration(new Dictionary<string, string?>
            {
                ["Prompts:ExtractDocumentContent"] = Path.GetRelativePath(AppContext.BaseDirectory, contentPath)
            }));

            Assert.Equal("initial prompt", PromptConfig.ExtractDocumentContent);

            File.WriteAllText(contentPath, "updated prompt");

            Assert.Equal("updated prompt", PromptConfig.ExtractDocumentContent);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
