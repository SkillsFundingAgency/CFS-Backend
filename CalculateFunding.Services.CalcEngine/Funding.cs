using System.Collections.Generic;
using CalculateFunding.Generators.Funding.Models;

namespace CalculateFunding.Services.CalcEngine
{
    public class Funding
    {
        public IDictionary<uint, string> Mappings { get; set; }
        public IEnumerable<FundingLine> FundingLines { get; set; }
    }
}
