using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace ILP.Shared.InfoExtraction.Model.Dto
{
    public class ChatCompletionResponseDto
    {
        [JsonProperty("id")]
        public string Id { get; set; } = string.Empty;

        [JsonProperty("object")]
        public string Object { get; set; } = string.Empty;

        [JsonProperty("created")]
        public long Created { get; set; }

        [JsonProperty("model")]
        public string Model { get; set; } = string.Empty;

        [JsonProperty("provider")]
        public string Provider { get; set; } = string.Empty;

        [JsonProperty("system_fingerprint")]
        public string? SystemFingerprint { get; set; }

        [JsonProperty("service_tier")]
        public string? ServiceTier { get; set; }

        [JsonProperty("choices")]
        public List<ChatChoiceDto> Choices { get; set; } = [];

        [JsonProperty("usage")]
        public ChatUsageDto? Usage { get; set; }
    }
}
