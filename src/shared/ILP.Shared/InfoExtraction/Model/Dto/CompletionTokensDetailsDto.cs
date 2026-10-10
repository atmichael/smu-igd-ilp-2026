using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace ILP.Shared.InfoExtraction.Model.Dto
{

    public class CompletionTokensDetailsDto
    {
        [JsonProperty("reasoning_tokens")]
        public int ReasoningTokens { get; set; }

        [JsonProperty("image_tokens")]
        public int ImageTokens { get; set; }

        [JsonProperty("audio_tokens")]
        public int AudioTokens { get; set; }
    }
}
