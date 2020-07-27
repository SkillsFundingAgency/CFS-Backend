using System.Collections.Generic;

namespace CalculateFunding.Models.Specs
{
    public class SpecificationCreateModel
    {
        public string FundingPeriodId { get; set; }

        public IEnumerable<string> FundingStreamIds { get; set; }

        public string ProviderVersionId { get; set; }

        public string Description { get; set; }

        public string Name { get; set; }

        public IDictionary<string, string> AssignedTemplateIds { get; set; }
    }
}
