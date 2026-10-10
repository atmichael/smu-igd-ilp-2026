using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace ILP.Shared.InfoExtraction.Model.Dto
{
    public class ChatUsageDto
    {
        [JsonProperty("prompt_tokens")]
        public int PromptTokens { get; set; }

        [JsonProperty("completion_tokens")]
        public int CompletionTokens { get; set; }

        [JsonProperty("total_tokens")]
        public int TotalTokens { get; set; }

        [JsonProperty("cost")]
        public decimal? Cost { get; set; }

        [JsonProperty("is_byok")]
        public bool? IsByok { get; set; }

        [JsonProperty("prompt_tokens_details")]
        public PromptTokensDetailsDto? PromptTokensDetails { get; set; }

        [JsonProperty("cost_details")]
        public CostDetailsDto? CostDetails { get; set; }

        [JsonProperty("completion_tokens_details")]
        public CompletionTokensDetailsDto? CompletionTokensDetails { get; set; }
    }
}
