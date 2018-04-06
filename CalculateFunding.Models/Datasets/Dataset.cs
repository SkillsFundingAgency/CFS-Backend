using CalculateFunding.Models.Versioning;

namespace CalculateFunding.Models.Datasets
{
    public class Dataset : VersionContainer<DatasetVersion>
    {
        public Reference Definition { get; set; }

        public string Description { get; set; }
    }
}
