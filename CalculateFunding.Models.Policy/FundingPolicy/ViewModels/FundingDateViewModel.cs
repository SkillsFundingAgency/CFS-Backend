using System.Collections.Generic;

namespace CalculateFunding.Models.Policy.FundingPolicy.ViewModels
{
    public class FundingDateViewModel
    {
        /// <summary>
        /// Funding date patterns
        /// </summary>
        public IEnumerable<FundingDatePattern> Patterns { get; set; }
    }
}
