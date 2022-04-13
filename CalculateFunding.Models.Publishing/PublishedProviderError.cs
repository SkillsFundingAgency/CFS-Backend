using System;

namespace CalculateFunding.Models.Publishing
{
    public class PublishedProviderError : IComparable
    {
        public string Identifier { get; set; }
        
        public PublishedProviderErrorType Type { get; set; }
        
        public string SummaryErrorMessage { get; set; }

        public string DetailedErrorMessage { get; set; }

        public string FundingLineCode { get; set; }

        public string FundingStreamId { get; set; }

        public int CompareTo(object obj)
        {
            if (GetHashCode() < obj?.GetHashCode()) return -1;
            if (GetHashCode() == obj?.GetHashCode()) return 0;
            return 1;
        }

        public override bool Equals(object obj)
        {
            return GetHashCode().Equals(obj?.GetHashCode());
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(
                Identifier,
                Type,
                SummaryErrorMessage,
                DetailedErrorMessage,
                FundingLineCode,
                FundingStreamId );
        }
    }
}