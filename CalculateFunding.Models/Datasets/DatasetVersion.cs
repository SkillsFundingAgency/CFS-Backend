using CalculateFunding.Models.Versioning;

namespace CalculateFunding.Models.Datasets
{
    public class DatasetVersion : VersionedItem
    {
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
