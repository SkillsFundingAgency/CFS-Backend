namespace CalculateFunding.Models.Datasets
{
    public class NewDatasetVersionResponseModel : CreateNewDatasetModel
    {
        public string BlobUrl { get; set;  }

        public string DatasetId { get; set; }

        public Reference Author { get; set; }

        public int Version { get; set; }
    }
}
