using CalculateFunding.Models.Versioning;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Datasets
{
    public class DatasetVersion : VersionedItem
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

        public string BlobName { get; set; }

        public int RowCount { get; set; }

        public override VersionedItem Clone()
        {
            return new DatasetVersion
            {
                RowCount = RowCount,
                PublishStatus = PublishStatus,
                Version = Version,
                Date = Date,
                Author = Author,
                Commment = Commment,
                BlobName = BlobName
            };
        }
    }
}
