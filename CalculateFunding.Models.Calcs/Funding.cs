using System.Collections.Generic;

namespace CalculateFunding.Models.Calcs
{
    public class Funding
    {
        public IDictionary<uint, string> Mappings { get; set; }
        public IEnumerable<FundingLine> FundingLines { get; set; }
    }
}
