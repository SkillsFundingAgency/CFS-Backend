using CalculateFunding.Common.Models;

namespace CalculateFunding.Models.Datasets.Converter
{
    public class SpecificationConverterMergeRequest
    {
        public string SpecificationId { get; set; }
        
        public Reference Author { get; set; }
    }
}