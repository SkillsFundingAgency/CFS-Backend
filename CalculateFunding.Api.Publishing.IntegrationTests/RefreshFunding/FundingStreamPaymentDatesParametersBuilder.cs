using CalculateFunding.Common.ApiClient.Publishing.Models;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Api.Publishing.IntegrationTests.RefreshFunding
{
    public class FundingStreamPaymentDatesParametersBuilder
        : TestEntityBuilder
    {
        private string _fundingPeriodId;
        private string _fundingStreamId;
        private FundingStreamPaymentDate[] _fundingStreamPaymentDates;

        public FundingStreamPaymentDatesParametersBuilder WithFundingStreamId(string fundingStreamId)
        {
            _fundingStreamId = fundingStreamId;

            return this;
        }

        public FundingStreamPaymentDatesParametersBuilder WithFundingPeriodId(string fundingPeriodId)
        {
            _fundingPeriodId = fundingPeriodId;

            return this;
        }

        public FundingStreamPaymentDatesParametersBuilder WithFundingStreamPaymentDates(
            params FundingStreamPaymentDate[] fundingStreamPaymentDates)
        {
            _fundingStreamPaymentDates = fundingStreamPaymentDates;

            return this;
        }

        public FundingStreamPaymentDatesParameters Build() =>
            new FundingStreamPaymentDatesParameters
            {
                FundingStreamId = _fundingStreamId ?? NewRandomString(),
                FundingPeriodId = _fundingPeriodId ?? NewRandomString(),
                PaymentDates = _fundingStreamPaymentDates
            };
    }
}
