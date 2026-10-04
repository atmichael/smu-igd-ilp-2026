using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace ILP.Shared.InfoExtraction.Model.Dto
{
    public class ChatChoiceDto
    {
        [JsonProperty("index")]
        public int Index { get; set; }

        [JsonProperty("logprobs")]
        public object? Logprobs { get; set; }

        [JsonProperty("finish_reason")]
        public string? FinishReason { get; set; }

        [JsonProperty("native_finish_reason")]
        public string? NativeFinishReason { get; set; }

        [JsonProperty("message")]
        public ChatResponseMessageDto Message { get; set; } = new();
    }
}
