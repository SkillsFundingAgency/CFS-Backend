using System.Collections.Generic;
using System.Linq;
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

        public int GetNextVersion()
        {
            if (History == null || !History.Any())
                return 1;

            int maxVersion = History.Max(m => m.Version);

            return maxVersion + 1;

        }
    }
}