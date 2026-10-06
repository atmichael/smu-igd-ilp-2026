using ILP.Server.Config;
using ILP.Shared.Helper;
using ILP.Shared.InfoExtraction.Model.Dto;
using Newtonsoft.Json;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;

namespace ILP.Shared.InfoExtraction.Provider
{
    public class EmailInfoExtractionProvider
    {

        public static async Task<string> GetDocumentContent(string emailBody, string attachmentPath = "", string parentTraceId = "", IReadOnlyDictionary<string, string>? inlineImages = null)
        {
            return await GetOpenRouterChatResponse(emailBody, attachmentPath, PromptConfig.ExtractDocumentContent, parentTraceId, inlineImages);
        }

        private static async Task<string> GetOpenRouterChatResponse(string emailBody, string attachmentPath, string systemPrompt, string parentTraceId = "", IReadOnlyDictionary<string, string>? inlineImages = null)
        {
            if (!HasExtractableInput(emailBody, attachmentPath, inlineImages))
            {
                return "";
            }

            string traceId = string.IsNullOrEmpty(parentTraceId) ? Guid.NewGuid().ToString() : parentTraceId;

            var request = await GetChatRequest(emailBody, attachmentPath, systemPrompt, inlineImages);
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

        internal static async Task<ChatRequestDto> GetChatRequest(string emailBody, string attachmentPath, string systemPrompt, IReadOnlyDictionary<string, string>? inlineImages = null)
        {

            // 1. Build email content placeholder 
            var builder = new StringBuilder();
            builder.AppendLine("Email Content:");
            var body = string.IsNullOrWhiteSpace(emailBody) ? "-" : emailBody;
            builder.AppendLine(ConvertHtmlToText(body).Trim());

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
                    if (mimeType.StartsWith("image/", StringComparison.Ordinal))
                    {
                        prompt.Content.Add(new ChatMessageContentDto()
                        {
                            Type = "image_url",
                            ImageUrl = new ChatMessageImageUrlDto() { Url = dataUri }
                        });
                    }
                    else
                    {
                        var fileInfo = new AttachmentFileDto() { FileName = Path.GetFileName(attachmentPath), FileData = dataUri };
                        var attachment = new ChatMessageContentDto() { Type = "file", File = fileInfo };
                        prompt.Content.Add(attachment);
                    }
                }
            }

            foreach (var imageSource in GetInlineImageSources(emailBody, inlineImages))
            {
                prompt.Content.Add(new ChatMessageContentDto()
                {
                    Type = "image_url",
                    ImageUrl = new ChatMessageImageUrlDto() { Url = imageSource }
                });
            }

            request.Messages.Add(prompt);
            return request;
        }

        internal static bool HasExtractableInput(string emailBody, string attachmentPath, IReadOnlyDictionary<string, string>? inlineImages = null)
        {
            if (!string.IsNullOrWhiteSpace(attachmentPath)
                && File.Exists(attachmentPath)
                && GetAttachmentMimeType(attachmentPath) != null)
            {
                return true;
            }

            return GetInlineImageSources(emailBody, inlineImages).Any()
                || IsExtractableDocumentText(ConvertHtmlToText(emailBody ?? ""));
        }

        private static bool IsExtractableDocumentText(string bodyText)
        {
            if (!Regex.IsMatch(bodyText, @"\b(?:invoice|receipt|bill|purchase\s+order|delivery\s+order|service\s+order|sales\s+order|statement\s+of\s+account|subtotal|total|amount\s+due|tax|gst|unit\s+price|quantity)\b", RegexOptions.IgnoreCase))
            {
                return false;
            }

            return Regex.IsMatch(bodyText, @"\d");
        }

        private static IEnumerable<string> GetInlineImageSources(string emailBody, IReadOnlyDictionary<string, string>? inlineImages)
        {
            var sources = new HashSet<string>(StringComparer.Ordinal);
            var imageTags = Regex.Matches(emailBody ?? "", @"<img\b[^>]*>", RegexOptions.IgnoreCase);

            foreach (Match imageTag in imageTags)
            {
                var sourceMatch = Regex.Match(
                    imageTag.Value,
                    @"\bsrc\s*=\s*(?:""(?<source>[^""]*)""|'(?<source>[^']*)'|(?<source>[^\s>]+))",
                    RegexOptions.IgnoreCase);

                if (!sourceMatch.Success)
                {
                    continue;
                }

                var source = WebUtility.HtmlDecode(sourceMatch.Groups["source"].Value).Trim();
                if (source.StartsWith("cid:", StringComparison.OrdinalIgnoreCase)
                    && TryGetInlineImage(source[4..], inlineImages, out var inlineImage))
                {
                    source = inlineImage;
                }

                if (IsSupportedImageSource(source))
                {
                    sources.Add(source);
                }
            }

            return sources;
        }

        private static bool TryGetInlineImage(string contentId, IReadOnlyDictionary<string, string>? inlineImages, out string imageSource)
        {
            imageSource = "";
            if (inlineImages == null)
            {
                return false;
            }

            contentId = contentId.Trim().Trim('<', '>');
            foreach (var inlineImage in inlineImages)
            {
                if (string.Equals(inlineImage.Key.Trim().Trim('<', '>'), contentId, StringComparison.OrdinalIgnoreCase))
                {
                    imageSource = inlineImage.Value;
                    return true;
                }
            }

            return false;
        }

        private static bool IsSupportedImageSource(string source)
        {
            if (source.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase))
            {
                var separator = source.IndexOf(',');
                if (separator < 0 || !source[..separator].EndsWith(";base64", StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                var mimeType = source[5..source.IndexOf(';')].ToLowerInvariant();
                if (mimeType is not ("image/png" or "image/jpeg" or "image/gif" or "image/webp"))
                {
                    return false;
                }

                var base64Image = source[(separator + 1)..];
                return base64Image.Length > 0
                    && Regex.IsMatch(
                        base64Image,
                        @"\A(?:[A-Za-z0-9+/]{4})*(?:[A-Za-z0-9+/]{2}==|[A-Za-z0-9+/]{3}=)?\z");
            }

            return Uri.TryCreate(source, UriKind.Absolute, out var uri)
                && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
        }

        private static string ConvertHtmlToText(string content)
        {
            content = Regex.Replace(content, @"<br\b[^>]*>|</(?:p|div|tr|li|h[1-6])\s*>", "\n", RegexOptions.IgnoreCase);
            content = Regex.Replace(content, @"</?(?:td|th)\b[^>]*>", " | ", RegexOptions.IgnoreCase);
            content = Regex.Replace(content, @"<[^>]+>", " ");
            content = WebUtility.HtmlDecode(content);
            content = Regex.Replace(content, @"[ \t]*\|[ \t]*", " | ");
            content = Regex.Replace(content, @"[ \t]*\n[ \t]*", "\n");
            content = Regex.Replace(content, @"\n{2,}", "\n");
            return content;
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
