namespace CalculateFunding.Models.Datasets
{
    public class GetDatasetBlobModel
    {
        public string DatasetId { get; set; }

        public string Filename { get; set; }

        public int Version { get; set; }

        public override string ToString()
        {
            return $"{DatasetId}/v{Version}/{Filename}";
        }
    }
}
