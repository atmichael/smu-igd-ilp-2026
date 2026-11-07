using ILP.Server.Config;
using ILP.Shared.Helper;
using ILP.Shared.InfoExtraction.Model.Dto;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;

namespace ILP.Shared.InfoExtraction.Provider;

internal static class OpenRouterChatClient
{
    public static async Task<string> GetCompletionAsync(
        ChatRequestDto request,
        string parentTraceId = "",
        CancellationToken cancellationToken = default)
    {
        string traceId = string.IsNullOrEmpty(parentTraceId) ? Guid.NewGuid().ToString() : parentTraceId;
        string requestJson = JsonConvert.SerializeObject(request);
        LogHelper.Trace(traceId, $"OpenRouterRequest model={request.Model}");

        using var client = new HttpClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", OpenRouterConfig.Key);
        client.DefaultRequestHeaders.Add("HTTP-Referer", "https://your-app-domain.com");
        client.DefaultRequestHeaders.Add("X-Title", ".NET 10 Native PDF Reader");
        client.DefaultRequestHeaders.Add("Trace-ID", traceId);

        using var content = new StringContent(requestJson, Encoding.UTF8, "application/json");
        using var response = await client.PostAsync(
            "https://openrouter.ai/api/v1/chat/completions", content, cancellationToken);
        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"OpenRouter API Error: {response.StatusCode}");
        }

        LogHelper.Trace(traceId, "OpenRouterResponse received");
        var completion = JsonConvert.DeserializeObject<ChatCompletionResponseDto>(responseJson);
        return completion is null ? "" : string.Join(",", completion.Choices.Select(choice => choice.Message.Content));
    }
}
