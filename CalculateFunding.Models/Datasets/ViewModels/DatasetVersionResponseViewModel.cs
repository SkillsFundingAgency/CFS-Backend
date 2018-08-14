using CalculateFunding.Models.Versioning;
using System;
using System.Collections.Generic;
using System.Text;

namespace CalculateFunding.Models.Datasets.ViewModels
{
    public class DatasetVersionResponseViewModel : Reference
    {
        public string BlobName { get; set; }

        public int Version { get; set; }

        public DateTimeOffset LastUpdatedDate { get; set; }

        public PublishStatus PublishStatus { get; set; }

        public Reference Definition { get; set; }

        public string Description { get; set; }

        public Reference Author { get; set; }

        public string Comment { get; set; }

        public string CurrentDataSourceRows { get; set; }

        public string PreviousDataSourceRows { get; set; }
    }
}
