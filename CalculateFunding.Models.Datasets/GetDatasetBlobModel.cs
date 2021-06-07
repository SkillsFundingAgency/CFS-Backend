namespace CalculateFunding.Models.Datasets
{
    public class GetDatasetBlobModel
    {
        public string DatasetId { get; set; }

        public string Filename { get; set; }

        public int Version { get; set; }

        public string DefinitionId { get; set; }

        public string Comment { get; set; }

        public string Description { get; set; }

        public string LastUpdatedById { get; set; }

        public string LastUpdatedByName { get; set; }

        public string FundingStreamId { get; set; }

        public bool MergeExistingVersion { get; set; }

        public DatasetEmptyFieldEvaluationOption EmptyFieldEvaluationOption { get; set; }

    }
}
