using System.Collections.Generic;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Specs
{
    public class Specification : Reference
    {

        [JsonProperty("academicYear")]
        public Reference AcademicYear { get; set; }

        [JsonProperty("fundingStream")]
        public Reference FundingStream { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("policies")]
        public List<PolicySpecification> Policies { get; set; }

    }

    public abstract class Command<T> where T : Reference
    {
        public Reference User { get; set; }
        public string Method { get; set; }

        public string TargetDocumentType { get; set; }

        public T Content { get; set; }
    }

    public class SpecificationCommand : Command<Specification>
    {
        
    }

    public class CalculationSpecificationCommand : Command<Specification>
    {
        public string SpecificationId { get; set; }
    }
}