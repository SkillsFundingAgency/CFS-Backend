namespace CalculateFunding.Services.DataImporter.Models
{
    public class DatasetDataTableMergeResult
    {
        public string TableDefinitionName { get; set; }
        public int NewRowsCount { get; set; }
        public int UpdatedRowsCount { get; set; }
    }
}
