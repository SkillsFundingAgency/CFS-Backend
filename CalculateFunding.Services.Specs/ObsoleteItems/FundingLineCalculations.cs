using System;
using System.Collections.Generic;
using System.Text;

namespace CalculateFunding.Services.Specs.ObsoleteItems
{
    public class FundingLineCalculation
    {
        public uint FundingLineId { get; set; }
        public IEnumerable<string> CalculationIds { get; set; }
    }
}
