using System;

namespace CalculateFunding.Models.Publishing
{
    public class ProfilingCarryOver
    {
        public string FundingLineCode { get; set; }

        public ProfilingCarryOverType Type { get; set; }

        public decimal Amount { get; set; }

        public override bool Equals(object obj) => obj?.GetHashCode().Equals(GetHashCode()) == true;

        public override int GetHashCode() => HashCode.Combine(FundingLineCode,
            Type,
            Amount);

        public override string ToString() => $"{FundingLineCode}:{Type}:{Amount}";
    }
}