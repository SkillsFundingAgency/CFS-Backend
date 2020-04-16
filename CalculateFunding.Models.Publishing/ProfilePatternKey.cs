using System;

namespace CalculateFunding.Models.Publishing
{
    public class ProfilePatternKey
    {
        public string FundingLineCode { get; set; }
        
        public string Key { get; set; }

        public override bool Equals(object obj)
        {
            return GetHashCode().Equals(obj?.GetHashCode());
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Key, FundingLineCode);
        }
    }
}