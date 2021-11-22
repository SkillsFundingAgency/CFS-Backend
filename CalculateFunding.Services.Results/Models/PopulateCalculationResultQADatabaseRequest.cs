using Newtonsoft.Json;

namespace CalculateFunding.Services.Results.Models
{
    public class PopulateCalculationResultQADatabaseRequest
    {
        [JsonProperty("specificationId")]
        public string SpecificationId { get; set; }
    }
}
