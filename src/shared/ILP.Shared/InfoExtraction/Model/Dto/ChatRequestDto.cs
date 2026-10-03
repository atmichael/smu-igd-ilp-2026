using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace ILP.Shared.InfoExtraction.Model.Dto
{
    public class ChatRequestDto
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = "";
        [JsonPropertyName("messages")]
        public List<ChatMessageDto> Messages { get; set; } = [];
    }
}
