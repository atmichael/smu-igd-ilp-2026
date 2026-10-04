using Newtonsoft.Json;

namespace ILP.Shared.InfoExtraction.Model.Dto
{
    public class ChatMessageImageUrlDto
    {
        [JsonProperty("url")]
        public string Url { get; set; } = "";
    }
}
