using System;
using System.Collections.Generic;
using System.Text;
using CalculateFunding.Generators.Funding.Models;

namespace CalculateFunding.Services.CalcEngine
{
    public class Funding
    {
        public IDictionary<uint, string> Mappings { get; set; }
        public IEnumerable<FundingLine> FundingLines { get; set; }
    }
}
