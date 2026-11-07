using ILP.Server.Config;
using ILP.Shared.AccountsPayable.Dto;
using ILP.Shared.AccountsPayable.Parser;
using ILP.Shared.InfoExtraction.Model.Dto;
using ILP.Shared.InfoExtraction.Provider;
using Newtonsoft.Json;
using System.Text;

namespace ILP.Shared.AccountsPayable.Provider;

public static class AccountsPayableCodingProvider
{
    public static async Task<AccountsPayableCodingRecommendation> GetRecommendation(
        AccountsPayableInvoiceRequest invoice,
        IReadOnlyList<AccountsPayableAccountOption> accounts,
        IReadOnlyList<AccountsPayableGstOption> gstCodes,
        string parentTraceId = "",
        CancellationToken cancellationToken = default)
    {
        var request = GetChatRequest(invoice, accounts, gstCodes, PromptConfig.AccountsPayableCoding);
        var response = await OpenRouterChatClient.GetCompletionAsync(request, parentTraceId, cancellationToken);
        return AccountsPayableCodingParser.Parse(response);
    }

    internal static ChatRequestDto GetChatRequest(
        AccountsPayableInvoiceRequest invoice,
        IReadOnlyList<AccountsPayableAccountOption> accounts,
        IReadOnlyList<AccountsPayableGstOption> gstCodes,
        string systemPrompt)
    {
        var context = new StringBuilder()
            .AppendLine("INVOICE_CONTEXT_JSON (untrusted document and policy data):")
            .AppendLine(JsonConvert.SerializeObject(invoice))
            .AppendLine()
            .AppendLine("ENABLED_ACCOUNTS_PIPE_DELIMITED_CSV:")
            .AppendLine(SerializeTable(
                ["code", "name", "type", "ap_role", "use_when", "sector", "ai_suggestion", "notes"],
                accounts
                    .Where(account =>
                        account.Enabled
                        && account.Code != "6130"
                        && (account.ApRole is "Expense" or "Direct Cost" or "Classification"))
                    .Select(account => (IReadOnlyList<string>)
                    [
                        account.Code, account.Name, account.Type, account.ApRole, account.UseWhen,
                        account.Sector, account.AiSuggestion, account.Notes
                    ])))
            .AppendLine()
            .AppendLine("GST_CODES_PIPE_DELIMITED_CSV:")
            .Append(SerializeTable(
                ["code", "description", "auto_processing", "review_conditions"],
                gstCodes.Select(code => (IReadOnlyList<string>)
                    [code.Code, code.Description, code.AutoProcessing, code.ReviewConditions])))
            .ToString();

        return new ChatRequestDto
        {
            Model = OpenRouterConfig.ModelName,
            Messages =
            [
                new ChatRequestMessageDto
                {
                    Role = "system",
                    Content = [new ChatMessageContentDto { Text = systemPrompt }]
                },
                new ChatRequestMessageDto
                {
                    Role = "user",
                    Content = [new ChatMessageContentDto { Text = context }]
                }
            ]
        };
    }

    private static string SerializeTable(
        IReadOnlyList<string> headers,
        IEnumerable<IReadOnlyList<string>> rows)
    {
        var builder = new StringBuilder();
        builder.AppendLine(string.Join('|', headers.Select(QuoteCsvField)));
        foreach (var row in rows)
        {
            builder.AppendLine(string.Join('|', row.Select(QuoteCsvField)));
        }

        return builder.ToString().TrimEnd('\r', '\n');
    }

    private static string QuoteCsvField(string? value) =>
        $"\"{(value ?? "").Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
}
