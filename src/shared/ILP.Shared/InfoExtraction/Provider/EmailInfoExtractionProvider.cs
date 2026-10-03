using ILP.Server.Config;
using ILP.Shared.InfoExtraction.Model.Dto;
using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace ILP.Shared.InfoExtraction.Provider
{
    public class EmailInfoExtractionProvider
    {
        public static async Task<string> GetDocumentHeader(string emailBody, List<string> attachmentPaths)
        {
            var builder = new StringBuilder();
            builder.AppendLine("Email Content:");
            var body = string.IsNullOrWhiteSpace(emailBody) ? "-" : emailBody;
            builder.AppendLine(body.Trim());

            var payload = new ChatRequestDto()
            {
                Model = OpenRouterConfig.ModelName,
                Messages = new List<ChatMessageDto>()

            };

            var prompt = new ChatMessageDto()
            {
                Role = "user",
                Content = new List<ChatMessageContentDto>()
                {
                    new ChatMessageContentDto() {Type = "text", Text = InfoExtractionPrompts.ExtractDocumentHeaderInfo},

                    new ChatMessageContentDto() {Type = "text", Text = builder.ToString()}
                }
            };

            foreach (var path in attachmentPaths)
            {
                byte[] pdfBytes = await File.ReadAllBytesAsync(path);
                string base64Pdf = Convert.ToBase64String(pdfBytes);
                string pdfDataUri = $"data:application/pdf;base64,{base64Pdf}";
                var fileInfo = new AttachmentFileDto() { FileName = Path.GetFileName(path), FileData = pdfDataUri };
                var attachment = new ChatMessageContentDto() { Type = "file", File = fileInfo };
                prompt.Content.Add(attachment);
            }

            payload.Messages.Add(prompt);

            string jsonPayload = JsonSerializer.Serialize(payload);

            Console.WriteLine($"Json payload: {jsonPayload}");

            // 3. Make HTTP request to OpenRouter API
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", OpenRouterConfig.Key);
                client.DefaultRequestHeaders.Add("HTTP-Referer", "https://your-app-domain.com");
                client.DefaultRequestHeaders.Add("X-Title", ".NET 10 Native PDF Reader");

                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await client.PostAsync("https://openrouter.ai/api/v1/chat/completions", content);
                string jsonResponse = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"OpenRouter API Error: {response.StatusCode} - {jsonResponse}");
                }

                Console.WriteLine($"==========================================");
                Console.WriteLine($"OpenRouter API Response: {jsonResponse}");
                Console.WriteLine($"==========================================");

                // 4. Extract and return content response
                using (JsonDocument doc = JsonDocument.Parse(jsonResponse))
                {
                    return doc.RootElement
                              .GetProperty("choices")
                              .GetProperty("message")
                              .GetProperty("content")
                              .GetString();
                }
            }
        }
    }
}
