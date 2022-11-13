using System.Text.Json.Serialization;

namespace BSAutoGenerator.Data.Structure
{
    internal class CustomDataInfo
    {
        [JsonConstructor]
        public CustomDataInfo(Editors _editors)
        {
            if(_editors == null)
            {
                _editors = new Editors("BSAutoGenerator", new());
            }
            this._editors = _editors;
        }

        [JsonInclude]
        [JsonPropertyName("_editors")]
        public Editors _editors { get; set; }

        internal class Editors
        {

            [JsonConstructor]
            public Editors(string _lastEditedBy, BSAutoGenerator BSAutoGenerator)
            {
                _lastEditedBy = "BSAutoGenerator";
                this._lastEditedBy = _lastEditedBy;

                BSAutoGenerator = new BSAutoGenerator();
                this.BSAutoGenerator = BSAutoGenerator;
            }

            [JsonInclude]
            [JsonPropertyName("_lastEditedBy")]
            public string _lastEditedBy { get; set; } = "BSAutoGenerator";
            [JsonInclude]
            [JsonPropertyName("BSAutoGenerator")]
            public BSAutoGenerator BSAutoGenerator { get; set; } = new();
        }

        internal class BSAutoGenerator
        {
            [JsonConstructor]
            public BSAutoGenerator(string version = "1.0.5")
            {
                this.version = version;
            }

            [JsonInclude]
            [JsonPropertyName("version")]
            public string version { get; set; } = "1.0.5";
        }
    }
}
