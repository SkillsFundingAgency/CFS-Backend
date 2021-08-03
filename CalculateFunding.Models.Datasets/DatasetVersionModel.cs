using CalculateFunding.Common.Models;
using System;

namespace CalculateFunding.Models.Datasets
{
    public class DatasetVersionModel
    {
        public string Id { get; set; }
        public string DatasetId { get; set; }
        public int Version { get; set; }
        public Reference Author { get; set; }
        public DateTimeOffset Date { get; set; }
        public string Comment { get; set; }
        public string Description { get; set; }
    }
}
