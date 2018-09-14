using System.Collections.Generic;
using CalculateFunding.Models.Versioning;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Results
{
    public class SourceDataset : VersionedItem
    {
        //AB: These 2 properties are not required yet, will be updated during the story

        [JsonProperty("id")]
        public override string Id
        {
            get { return ""; }
        }

        [JsonProperty("entityId")]
        public override string EntityId
        {
            get { return ""; }
        }

        [JsonProperty("dataset")]
        public VersionReference Dataset { get; set; }
        [JsonProperty("rows")]
        public List<Dictionary<string, object>> Rows { get; set; }
        [JsonProperty("checksum")]
        public string Checksum { get; set; }

        public override VersionedItem Clone()
        {
            
            return new SourceDataset
            {
                PublishStatus = PublishStatus,
                Version = Version,
                Date = Date,
                Author = Author,
                Commment = Commment,
                Dataset = new VersionReference(Dataset?.Id, Dataset?.Name, Dataset?.Version ?? 0),
                Rows = Rows == null ? Rows : new List<Dictionary<string, object>>(), // TODO Clone properly
                Checksum = Checksum
            };
        }
    }
}