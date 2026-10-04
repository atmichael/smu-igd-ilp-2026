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
