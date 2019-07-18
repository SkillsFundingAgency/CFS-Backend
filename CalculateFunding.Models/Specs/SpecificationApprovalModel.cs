using System;
using System.Collections.Generic;
using System.Text;

namespace CalculateFunding.Models.Specs
{
    public class SpecificationApprovalModel
    {
        public string FundingStreamId { get; set; }

        public IEnumerable<string> Providers { get; set; }
    }
}
