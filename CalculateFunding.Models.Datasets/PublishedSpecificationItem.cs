using CalculateFunding.Models.Datasets.Schema;

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
    }
}
