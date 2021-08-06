namespace CalculateFunding.Models.Datasets
{
    public class CreateNewDatasetModel
    {
        public string DefinitionId { get; set; }

        public string Filename { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public string FundingStreamId { get; set; }

        public int RowCount { get; set; }

        public bool StrictValidation { get; set; } = true;
    }
}
