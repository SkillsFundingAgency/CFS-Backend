namespace CalculateFunding.Models.Datasets
{
    public class CreateNewDatasetResponseModel : CreateNewDatasetModel
    {
        public string BlobUrl { get; set;  }

        public string DatasetId { get; set; }

        public Reference Author { get; set; }
    }
}
