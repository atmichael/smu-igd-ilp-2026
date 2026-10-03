using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace ILP.Shared.InfoExtraction.Model.Dto
{
    public class ChatMessageContentDto
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "text";
        [JsonPropertyName("text")]
        public string? Text { get; set; } = null;

        [JsonPropertyName("file")]
        public AttachmentFileDto? File { get; set; } = null;
    }
}
