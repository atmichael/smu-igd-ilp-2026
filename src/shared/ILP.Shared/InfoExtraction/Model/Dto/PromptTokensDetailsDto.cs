using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace ILP.Shared.InfoExtraction.Model.Dto
{
    public class PromptTokensDetailsDto
    {
        [JsonProperty("cached_tokens")]
        public int CachedTokens { get; set; }

        [JsonProperty("cache_write_tokens")]
        public int CacheWriteTokens { get; set; }

        [JsonProperty("audio_tokens")]
        public int AudioTokens { get; set; }

        [JsonProperty("video_tokens")]
        public int VideoTokens { get; set; }
    }
}
