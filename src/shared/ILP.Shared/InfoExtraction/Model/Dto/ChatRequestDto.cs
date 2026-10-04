using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace ILP.Shared.InfoExtraction.Model.Dto
{
    public class ChatRequestDto
    {
        [JsonProperty("model")]
        public string Model { get; set; } = "";
        [JsonProperty("messages")]
        public List<ChatRequestMessageDto> Messages { get; set; } = [];
    }
}
