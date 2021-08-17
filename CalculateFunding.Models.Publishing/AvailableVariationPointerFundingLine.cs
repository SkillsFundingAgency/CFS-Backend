using System.Collections.Generic;

namespace CalculateFunding.Models.Publishing
{
    public class AvailableVariationPointerFundingLine
    {
        public string FundingLineCode { get; set; }

        public string FundingLineName { get; set; }

        public IEnumerable<AvailableVariationPointerProfilePeriod> Periods { get; set; }

        public AvailableVariationPointerProfilePeriod SelectedPeriod { get; set; }
    }
}
