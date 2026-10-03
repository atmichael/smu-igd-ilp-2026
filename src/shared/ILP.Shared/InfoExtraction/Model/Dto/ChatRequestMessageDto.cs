using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace ILP.Shared.InfoExtraction.Model.Dto
{
    public class ChatRequestMessageDto
    {

        [JsonProperty("role")]
        public string Role { get; set; } = "user";

        [JsonProperty("content")]
        public List<ChatMessageContentDto> Content { get; set; } = [];

    }
}
