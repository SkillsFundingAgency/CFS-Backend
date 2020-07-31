using System;

namespace CalculateFunding.Repositories.Common.Search.Results
{
    public class CalculationProviderResultSearchResult
    {
        public string Id { get; set; }

        public string ProviderId { get; set; }

        public string ProviderName { get; set; }

        public string SpecificationId { get; set; }

        public string SpecificationName { get; set; }

        public DateTimeOffset LastUpdatedDate { get; set; }

        public string LocalAuthority { get; set; }

        public string ProviderType { get; set; }

        public string ProviderSubType { get; set; }

        public string UKPRN { get; set; }

        public string URN { get; set; }

        public string UPIN { get; set; }

        public DateTimeOffset? OpenDate { get; set; }

        public string EstablishmentNumber { get; set; }

        public string CalculationId { get; set; }

        public string CalculationName { get; set; }

        public object CalculationResult { get; set; }

        public string CalculationExceptionType { get; set; }

        public string CalculationExceptionMessage { get; set; }

        public string FundingLineId { get; set; }

        public string FundingLineName { get; set; }

        public decimal? FundingLineResult { get; set; }

        public string FundingLineExceptionType { get; set; }

        public string FundingLineExceptionMessage { get; set; }
    }
}
