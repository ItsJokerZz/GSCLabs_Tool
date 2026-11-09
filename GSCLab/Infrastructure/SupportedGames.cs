using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GSCLab.Infrastructure
{
    public class SupportedGame
    {
        [JsonPropertyName("GameName")]
        public string GameName { get; set; }

        [JsonPropertyName("ShortName")]
        public string ShortName { get; set; }

        [JsonPropertyName("Modes")]
        public List<string> Modes { get; set; }

        [JsonPropertyName("Consoles")]
        public List<string> Consoles { get; set; }
    }
}