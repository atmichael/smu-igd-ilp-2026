using ILP.Server.Config;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;

namespace ILP.Shared.Test.InfoExtraction.Config;

public class PromptConfigTest
{
    [Fact]
    public void PromptProperties_ReadUpdatedFileContentWithoutReinitializing()
    {
        var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(directory);

        try
        {
            var contentPath = Path.Combine(directory, "content.md");
            var headerInfoPath = Path.Combine(directory, "header.md");
            var lineItemInfoPath = Path.Combine(directory, "line-items.md");
            File.WriteAllText(contentPath, "initial prompt");
            File.WriteAllText(headerInfoPath, "header prompt");
            File.WriteAllText(lineItemInfoPath, "line-item prompt");

            PromptConfig.Initialize(new TestConfiguration(new Dictionary<string, string?>
            {
                ["Prompts:ExtractDocumentContent"] = Path.GetRelativePath(AppContext.BaseDirectory, contentPath),
                ["Prompts:ExtractDocumentHeaderInfo"] = Path.GetRelativePath(AppContext.BaseDirectory, headerInfoPath),
                ["Prompts:ExtractDocumentLineItemInfo"] = Path.GetRelativePath(AppContext.BaseDirectory, lineItemInfoPath)
            }));

            Assert.Equal("initial prompt", PromptConfig.ExtractDocumentContent);
            Assert.Equal("header prompt", PromptConfig.ExtractDocumentHeaderInfo);
            Assert.Equal("line-item prompt", PromptConfig.ExtractDocumentLineItemInfo);

            File.WriteAllText(contentPath, "updated prompt");

            Assert.Equal("updated prompt", PromptConfig.ExtractDocumentContent);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private sealed class TestConfiguration(Dictionary<string, string?> values) : IConfiguration
    {
        public string? this[string key]
        {
            get => values.GetValueOrDefault(key);
            set => values[key] = value;
        }

        public IEnumerable<IConfigurationSection> GetChildren() => [];

        public IChangeToken GetReloadToken() => throw new NotSupportedException();

        public IConfigurationSection GetSection(string key) => throw new NotSupportedException();
    }
}
