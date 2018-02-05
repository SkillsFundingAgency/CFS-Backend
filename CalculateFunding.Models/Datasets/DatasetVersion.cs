using CalculateFunding.Models.Versioning;

namespace CalculateFunding.Models.Datasets
{
    public class DatasetVersion : VersionedItem
    {
        public override VersionedItem Clone()
        {
            return new DatasetVersion
            {
                PublishStatus = PublishStatus,
                Version = Version,
                Date = Date,
                Author = Author,
                Commment = Commment
            };
        }
    }
}
