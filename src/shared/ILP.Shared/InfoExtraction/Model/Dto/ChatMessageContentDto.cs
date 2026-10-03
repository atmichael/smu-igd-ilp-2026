using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace ILP.Shared.InfoExtraction.Model.Dto
{
    public class ChatMessageContentDto
    {
        [JsonProperty("type")]
        public string Type { get; set; } = "text";
        [JsonProperty("text")]
        public string? Text { get; set; } = null;

        [JsonProperty("file")]
        public AttachmentFileDto? File { get; set; } = null;
    }
}
