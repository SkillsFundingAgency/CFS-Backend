using CalculateFunding.Models.Specs;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Datasets
{
    public class SourceDefinition : Reference
    {
        [JsonProperty("academicYear")]
        public Reference AcademicYear { get; set; }

        [JsonProperty("rowLevel")]
        public RowLevel RowLevel { get; set; }

        public DatasetDefinition DatasetDefinition { get; set; }

    }
}