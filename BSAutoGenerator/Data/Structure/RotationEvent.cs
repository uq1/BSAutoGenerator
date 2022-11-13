using System.Text.Json.Serialization;

namespace BSAutoGenerator.Data.Structure
{
    internal class RotationEvent
    {
        [JsonInclude]
        [JsonPropertyName("b")]
        public float beat { get; set; }
        [JsonInclude]
        [JsonPropertyName("e")]
        public int executionTime { get; set; }
        [JsonInclude]
        [JsonPropertyName("r")]
        public float rotation { get; set; }
    }
}
