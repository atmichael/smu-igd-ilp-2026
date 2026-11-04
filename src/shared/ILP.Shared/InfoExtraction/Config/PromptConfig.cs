using Microsoft.Extensions.Configuration;

namespace ILP.Server.Config;

public static class PromptConfig
{
    private static string m_extractDocumentContentPath = string.Empty;
    private static string m_accountsPayableCodingPath = string.Empty;

    public static string ExtractDocumentContent { get { return File.ReadAllText(m_extractDocumentContentPath); } }
    public static string AccountsPayableCoding { get { return File.ReadAllText(m_accountsPayableCodingPath); } }

    public static void Initialize(IConfiguration config)
    {
        var contentPath = GetPromptPath(config, nameof(ExtractDocumentContent));

        m_extractDocumentContentPath = contentPath;
        var apCodingPath = config[$"Prompts:{nameof(AccountsPayableCoding)}"];
        m_accountsPayableCodingPath = string.IsNullOrWhiteSpace(apCodingPath)
            ? Path.Combine(AppContext.BaseDirectory, "AccountsPayable", "Prompts", "AccountsPayableCoding.md")
            : Path.GetFullPath(apCodingPath, AppContext.BaseDirectory);
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
