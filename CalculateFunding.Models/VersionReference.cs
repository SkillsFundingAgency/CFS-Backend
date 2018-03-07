using Newtonsoft.Json;

namespace CalculateFunding.Models
{
    public class VersionReference : Reference
    {
        public VersionReference(string id, string name, int version) : base(id, name)
        {
            Version = version;
        }

        [JsonProperty("version")]
        public int Version { get; set; }
    }
}
