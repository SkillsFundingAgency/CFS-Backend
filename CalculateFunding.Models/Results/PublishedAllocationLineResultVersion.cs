﻿using CalculateFunding.Models.Versioning;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CalculateFunding.Models.Results
{
    public class PublishedAllocationLineResultVersion
    {
        [JsonProperty("status")]
        public AllocationLineStatus Status { get; set; }

        [JsonProperty("version")]
        public int Version { get; set; }

        [JsonProperty("date")]
        public DateTimeOffset Date { get; set; }

        [JsonProperty("author")]
        public Reference Author { get; set; }

        [JsonProperty("comment")]
        public string Commment { get; set; }

        public PublishedAllocationLineResultVersion Clone()
        {
            // Serialise to perform a deep copy
            string json = JsonConvert.SerializeObject(this);
            return JsonConvert.DeserializeObject<PublishedAllocationLineResultVersion>(json);
        }
    }
}
