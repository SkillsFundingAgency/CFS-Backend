using CalculateFunding.Common.Models;
using CalculateFunding.Models.Versioning;

namespace CalculateFunding.Models.Datasets
{
    public class Dataset : VersionContainer<DatasetVersion>
    {
        public DatasetDefinitionVersion Definition { get; set; }

        public string Description { get; set; }
    }
}
