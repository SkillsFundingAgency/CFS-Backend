using CalculateFunding.Common.Models;

namespace CalculateFunding.Models.Datasets.Converter
{
    public class ConverterMergeRequest
    {
        public string ProviderVersionId { get; set; }

        public string DatasetId { get; set; }

        public string Version { get; set; }

        public Reference Author { get; set; }

        public string DatasetRelationshipId { get; set; }
    }
}
