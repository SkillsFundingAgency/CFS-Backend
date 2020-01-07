using System;
using System.Collections.Generic;

namespace CalculateFunding.Models.Datasets
{
    public class DatasetMetadataModel
    {
        public string AuthorName { get; set; }
        public string AuthorId { get; set; }
        public string DatasetId { get; set; }
        public string DataDefinitionId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Comment { get; set; }
    }
}
