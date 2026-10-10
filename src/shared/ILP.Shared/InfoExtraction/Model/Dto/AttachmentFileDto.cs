using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace ILP.Shared.InfoExtraction.Model.Dto
{
    public class AttachmentFileDto
    {
        [JsonProperty("file_name")]
        public string FileName { get; set; } = "attachment";
        [JsonProperty("file_data")]
        public string FileData { get; set; } = "";

        public AttachmentFileDto() { }
    }
}
