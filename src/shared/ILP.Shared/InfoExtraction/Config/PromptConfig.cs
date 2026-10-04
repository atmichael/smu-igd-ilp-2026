using Microsoft.Extensions.Configuration;

namespace ILP.Server.Config;

public static class PromptConfig
{
    private static string m_extractDocumentContentPath = string.Empty;
    private static string m_extractDocumentHeaderInfoPath = string.Empty;
    private static string m_extractDocumentLineItemInfoPath = string.Empty;

    public static string ExtractDocumentContent => File.ReadAllText(m_extractDocumentContentPath);
    public static string ExtractDocumentHeaderInfo => File.ReadAllText(m_extractDocumentHeaderInfoPath);
    public static string ExtractDocumentLineItemInfo => File.ReadAllText(m_extractDocumentLineItemInfoPath);

    public static void Initialize(IConfiguration config)
    {
        var contentPath = GetPromptPath(config, nameof(ExtractDocumentContent));
        var headerInfoPath = GetPromptPath(config, nameof(ExtractDocumentHeaderInfo));
        var lineItemInfoPath = GetPromptPath(config, nameof(ExtractDocumentLineItemInfo));

        m_extractDocumentContentPath = contentPath;
        m_extractDocumentHeaderInfoPath = headerInfoPath;
        m_extractDocumentLineItemInfoPath = lineItemInfoPath;
    }

    private static string GetPromptPath(IConfiguration config, string promptName)
    {
        var promptPath = config[$"Prompts:{promptName}"];
        if (string.IsNullOrWhiteSpace(promptPath))
        {
            throw new ApplicationException($"Missing mandatory: Prompts:{promptName}");
        }

        return Path.GetFullPath(promptPath, AppContext.BaseDirectory);
    }
}
