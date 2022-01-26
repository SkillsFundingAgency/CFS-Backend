using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Models.Publishing
{
    public class ReProfileAudit
    {
        public string FundingLineCode { get; set; }

        public string ETag { get; set; }

        public string StrategyConfigKey { get; set; }

        public string Strategy { get; set; }

        public int? VariationPointerIndex { get; set; }

        public override bool Equals(object obj)
        {
            return GetHashCode().Equals(obj?.GetHashCode());
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(FundingLineCode, ETag, StrategyConfigKey, Strategy, VariationPointerIndex);
        }
    }
}
