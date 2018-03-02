using Newtonsoft.Json;

namespace CalculateFunding.Models
{
    public class VersionReference : Reference
    {
        public VersionReference(string id, string name, string version) : base(id, name)
        {
            Version = version;
        }

        [JsonProperty("version")]
        public string Version { get; set; }
    }
}
