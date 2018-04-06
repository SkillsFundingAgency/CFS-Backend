using System.Collections.Generic;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Versioning
{
    public abstract class VersionContainer<T> : Reference where T : VersionedItem
    {
        [JsonProperty("current")]
        public T Current { get; set; }

        [JsonProperty("published")]
        public T Published { get; set; }

        [JsonProperty("history")]
        public List<T> History { get; set; }
    }
}