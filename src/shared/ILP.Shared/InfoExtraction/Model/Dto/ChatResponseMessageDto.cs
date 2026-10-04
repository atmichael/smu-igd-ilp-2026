using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace ILP.Shared.InfoExtraction.Model.Dto
{
    public class ChatResponseMessageDto
    {

        [JsonProperty("role")]
        public string Role { get; set; } = "user";

        [JsonProperty("content")]
        public string Content { get; set; } = "";

        [JsonProperty("refusal")]
        public string? Refusal { get; set; }

        [JsonProperty("reasoning")]
        public string? Reasoning { get; set; }
    }
}
