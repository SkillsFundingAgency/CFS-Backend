using System;
using System.Collections.Generic;

namespace CalculateFunding.Models.Specs
{
    public class SpecificationSearchModel : Reference
    {
        public IEnumerable<Reference> FundingStreams { get; set; }

        public Reference FundingPeriod { get; set; }

        public DateTimeOffset UpdatedAt { get; set; }

        public string PublishStatus { get; set; }

        public string Description { get; set; }

        public string[] DataDefinitionRelationshipIds { get; set; }

        public bool IsSelectedForFunding { get; set; }
    }
}
