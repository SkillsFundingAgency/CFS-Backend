using System.Collections.Generic;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Models.Datasets.Schema;
using Newtonsoft.Json;

namespace CalculateFunding.Services.DataImporter
{
    public class RowLoadResult
    {
        [JsonProperty("identifier")]
        public string Identifier { get; set; }
        [JsonProperty("identifierFieldType")]
        public IdentifierFieldType IdentifierFieldType { get; set; }
        [JsonProperty("fields")]
        public Dictionary<string, object> Fields { get; set; }
        [JsonProperty("validationErrors")]
        public List<DatasetValidationError> ValidationErrors { get; set; }
    }
}