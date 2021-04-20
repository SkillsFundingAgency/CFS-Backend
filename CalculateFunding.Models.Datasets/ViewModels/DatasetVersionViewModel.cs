using CalculateFunding.Common.Models;

namespace CalculateFunding.Models.Datasets.ViewModels
{
    public class DatasetVersionViewModel
    {
        public Reference Author { get; set; }

        public int Version { get; set; }

        public string BlobName { get; set; }
        
        public DatasetChangeType ChangeType { get; set; }

        public Reference FundingStream { get; set; }
    }
}
