using CalculateFunding.Common.Models;
using CalculateFunding.Models.Datasets.Schema;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Datasets
{
    public class DatasetValidationError
    {
        public DatasetValidationError()
        {

        }

        public DatasetValidationError(FieldDefinition field, int row, string errorMessage)
        {
            Field = new Reference(field?.Id, field?.Name);
            Row = row;
            ErrorMessage = errorMessage;
        }
        [JsonProperty("row")]
        public int Row { get; set; }
        [JsonProperty("field")]
        public Reference Field { get; set; }
        [JsonProperty("errorMessage")]
        public string ErrorMessage { get; set; }
    }
}