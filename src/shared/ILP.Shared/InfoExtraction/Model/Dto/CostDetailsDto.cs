using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace ILP.Shared.InfoExtraction.Model.Dto
{
    public class CostDetailsDto
    {
        [JsonProperty("upstream_inference_cost")]
        public decimal UpstreamInferenceCost { get; set; }

        [JsonProperty("upstream_inference_prompt_cost")]
        public decimal UpstreamInferencePromptCost { get; set; }

        [JsonProperty("upstream_inference_completions_cost")]
        public decimal UpstreamInferenceCompletionsCost { get; set; }
    }
}
