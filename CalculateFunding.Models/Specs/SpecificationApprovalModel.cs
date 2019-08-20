using System;
using System.Collections.Generic;

namespace CalculateFunding.Models.Specs
{
    [Obsolete]
    public class SpecificationApprovalModel
    {
        public string FundingStreamId { get; set; }

        public IEnumerable<string> Providers { get; set; }
    }
}
