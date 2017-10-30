using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace Allocation.Models
{
    public class Reference
    {
        [JsonProperty("id")]
        public string Id { get; }
        [JsonProperty("name")]
        public string Name { get; }

        public Reference(string id, string name)
        {
            Id = id;
            Name = name;
        }
    }
}
