using System;
using System.Collections.Generic;

namespace CalculateFunding.Models.Specs
{
    public class SpecificationSearchModel : Reference
    {
        public IEnumerable<Reference> FundingStreams { get; set; }

        public Reference FundingPeriod { get; set; }

        public DateTime UpdatedAt { get; set; }

        public string[] DataDefinitionRelationshipIds { get; set; }
    }
}
