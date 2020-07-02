using CalculateFunding.Common.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace CalculateFunding.Models.Datasets.Schema
{
    public class DatasetDefinationByFundingStream
    {
        [JsonProperty("id")]
        public string Id { get; set; }
     
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("description")]        
        public string Description { get; set; }

    }
}
