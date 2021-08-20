using CalculateFunding.Models.Datasets.Schema;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Datasets
{
    public class PublishedSpecificationItem
    {
        public uint TemplateId { get; set; }

        public string Name { get; set; }

        public string SourceCodeName { get; set; }

        public FieldType FieldType { get; set; }

        public bool IsObsolete { get; set; }

        public bool IsSelected { get; set; }

        public bool IsUsedInCalculation { get; set; }

        [JsonIgnore]
        public string CalculationId { get; set; }
    }
}
