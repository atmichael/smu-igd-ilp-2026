using ILP.Server.Config;
using ILP.Shared.InfoExtraction.Model.Dto;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Text;
using System.Linq;
using ILP.Shared.Helper;

namespace ILP.Shared.InfoExtraction.Provider
{
    public class EmailInfoExtractionProvider
    {
        public static async Task<string> GetDocumentHeader(string emailBody, string attachmentPath = "", string parentTraceId = "")
        {
            return await GetOpenRouterChatResponse(emailBody, attachmentPath, InfoExtractionPrompts.ExtractDocumentHeaderInfo);
        }

        public static async Task<string> GetDocumentLineItem(string emailBody, string attachmentPath = "", string parentTraceId = "")
        {
            return await GetOpenRouterChatResponse(emailBody, attachmentPath, InfoExtractionPrompts.ExtractDocumentLineItemInfo);
        }

        public static async Task<string> GetDocumentContent(string emailBody, string attachmentPath = "", string parentTraceId = "")
        {
            return await GetOpenRouterChatResponse(emailBody, attachmentPath, InfoExtractionPrompts.ExtractDocumentContent);
        }

        private static async Task<string> GetOpenRouterChatResponse(string emailBody, string attachmentPath, string systemPrompt, string parentTraceId = "")
        {
            string traceId = string.IsNullOrEmpty(parentTraceId) ? Guid.NewGuid().ToString() : parentTraceId;

            var request = await GetChatRequest(emailBody, attachmentPath, systemPrompt);
            string requestJson = JsonConvert.SerializeObject(request);

            LogHelper.Trace(traceId, $"OpenRouterRequest: {requestJson}");

            // 3. Make HTTP request to OpenRouter API
            string responseJson = "";
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", OpenRouterConfig.Key);
                client.DefaultRequestHeaders.Add("HTTP-Referer", "https://your-app-domain.com");
                client.DefaultRequestHeaders.Add("X-Title", ".NET 10 Native PDF Reader");
                client.DefaultRequestHeaders.Add("Trace-ID", traceId);

                var content = new StringContent(requestJson, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await client.PostAsync("https://openrouter.ai/api/v1/chat/completions", content);
                responseJson = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"OpenRouter API Error: {response.StatusCode} - {responseJson}");
                }

                LogHelper.Trace(traceId, $"OpenRouterResponse: {responseJson}");
            }

            // 4. Extract and return content response
            var doc = JsonConvert.DeserializeObject<ChatCompletionResponseDto>(responseJson);
            return doc == null ? "" : string.Join(",", doc.Choices.Select(s => s.Message.Content).ToArray());
        }

        internal static async Task<ChatRequestDto> GetChatRequest(string emailBody, string attachmentPath, string systemPrompt)
        {

            // 1. Build email content placeholder 
            var builder = new StringBuilder();
            builder.AppendLine("Email Content:");
            var body = string.IsNullOrWhiteSpace(emailBody) ? "-" : emailBody;
            builder.AppendLine(body.Trim());

            // 2. Build OpenRouter request message 
            var request = new ChatRequestDto()
            {
                Model = OpenRouterConfig.ModelName,
                Messages = new List<ChatRequestMessageDto>()
            };

            var prompt = new ChatRequestMessageDto()
            {
                Role = "user",
                Content = new List<ChatMessageContentDto>()
                {
                    new ChatMessageContentDto() {Type = "text", Text = systemPrompt },
                    new ChatMessageContentDto() {Type = "text", Text = builder.ToString()}
                }
            };

            if (!string.IsNullOrEmpty(attachmentPath) && File.Exists(attachmentPath))
            {
                string? mimeType = GetAttachmentMimeType(attachmentPath);
                if (mimeType != null)
                {
                    byte[] attachmentBytes = await File.ReadAllBytesAsync(attachmentPath);
                    string base64Attachment = Convert.ToBase64String(attachmentBytes);
                    string dataUri = $"data:{mimeType};base64,{base64Attachment}";
                    var fileInfo = new AttachmentFileDto() { FileName = Path.GetFileName(attachmentPath), FileData = dataUri };
                    var attachment = new ChatMessageContentDto() { Type = "file", File = fileInfo };
                    prompt.Content.Add(attachment);
                }
            }

            request.Messages.Add(prompt);
            return request;
        }

        private static string? GetAttachmentMimeType(string attachmentPath)
        {
            return Path.GetExtension(attachmentPath).ToLowerInvariant() switch
            {
                ".pdf" => "application/pdf",
                ".png" => "image/png",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".xls" => "application/vnd.ms-excel",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                _ => null
            };
        }
    }
}
