using System.Collections.Generic;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Models.Datasets.Schema;
using Newtonsoft.Json;

namespace CalculateFunding.Services.DataImporter
{
    public class TableLoadResult
    {
        [JsonProperty("tableDefinition")]
        public TableDefinition TableDefinition { get; set; }

        [JsonProperty("globalErrors")]
        public List<DatasetValidationError> GlobalErrors { get; set; }

        [JsonProperty("rows")]
        public List<RowLoadResult> Rows { get; set; }
    }
}