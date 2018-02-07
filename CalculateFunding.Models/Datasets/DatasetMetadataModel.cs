using System;
using System.Collections.Generic;
using System.Text;

namespace CalculateFunding.Models.Datasets
{
    public class DatasetMetadataModel
    {
        public DatasetMetadataModel(){}

        public DatasetMetadataModel(IDictionary<string, string> metadData)
        {
            if (metadData == null)
                throw new ArgumentNullException(nameof(metadData));

            AuthorName = metadData.ContainsKey("authorName") ? metadData["authorName"] : string.Empty;
            AuthorId = metadData.ContainsKey("authorId") ? metadData["authorId"] : string.Empty;
            DatasetId = metadData.ContainsKey("datasetId") ? metadData["datasetId"] : string.Empty;
            DataDefinitionId = metadData.ContainsKey("dataDefinitionId") ? metadData["dataDefinitionId"] : string.Empty;
            Name = metadData.ContainsKey("name") ? metadData["name"] : string.Empty;
            Description = metadData.ContainsKey("description") ? metadData["description"] : string.Empty;
        }

        public string AuthorName { get; set; }
        public string AuthorId { get; set; }
        public string DatasetId { get; set; }
        public string DataDefinitionId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }
}
