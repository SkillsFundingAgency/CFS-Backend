using System.Collections.Generic;

namespace CalculateFunding.Models.Datasets.ViewModels
{
    public class DatasetMetadataViewModel
    {
        public string DataDefinitionId { get; set; }
        public string DatasetId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string FundingStreamId { get; set; }
        public string AuthorId { get; set; }
        public string AuthorName { get; set; }
        public int? Version { get; set; }
        public IEnumerable<RelationshipDataSetExcelData> ExcelData { get; set; }
        public bool ConverterEligible { get; set; }
    }
}
