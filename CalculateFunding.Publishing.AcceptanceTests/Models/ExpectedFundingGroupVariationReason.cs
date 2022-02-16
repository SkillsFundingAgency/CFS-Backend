using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Publishing.AcceptanceTests.Models
{
    internal class ExpectedFundingGroupVariationReason
    {
        public int FundingGroupVersionVariationReasonId { get; set; }

        public string VariationReason { get; set; }

        public int FundingGroupVersionId { get; set; }
    }
}
