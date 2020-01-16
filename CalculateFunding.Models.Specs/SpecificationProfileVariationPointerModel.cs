using System;
using System.Collections.Generic;
using System.Text;

namespace CalculateFunding.Models.Specs
{
    public class SpecificationProfileVariationPointerModel
    {
        public string FundingStreamId { get; set; }

        public string FundingLineId { get; set; }

        public string PeriodType { get; set; }

        public string TypeValue { get; set; }

        public int Year { get; set; }

        public int Occurrence { get; set; }
    }
}
