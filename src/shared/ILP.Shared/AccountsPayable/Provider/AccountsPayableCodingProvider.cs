using ILP.Server.Config;
using ILP.Shared.AccountsPayable.Dto;
using ILP.Shared.AccountsPayable.Parser;
using ILP.Shared.InfoExtraction.Model.Dto;
using ILP.Shared.InfoExtraction.Provider;
using Newtonsoft.Json;

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
        var context = new
        {
            invoice,
            enabled_accounts = accounts.Where(account =>
                account.Enabled
                && account.Code != "6130"
                && (account.ApRole is "Expense" or "Direct Cost" or "Classification")),
            gst_codes = gstCodes
        };
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
                    Content = [new ChatMessageContentDto { Text = JsonConvert.SerializeObject(context) }]
                }
            ]
        };
    }
}
