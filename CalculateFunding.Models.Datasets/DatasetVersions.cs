using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Common.Models;

namespace CalculateFunding.Models.Datasets
{
    public class DatasetVersions : Reference
    {
        public DatasetVersions()
        {
            Versions = Enumerable.Empty<DatasetVersionModel>();
        }
        public string Description { get; set; }

        public int? SelectedVersion { get; set; }

        public IEnumerable<DatasetVersionModel> Versions { get; set; }
    }
}
