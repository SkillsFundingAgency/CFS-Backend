using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Publishing.UnitTests.Profiling
{
    public class FundingStreamPaymentDatesBuilder : TestEntityBuilder
    {
        private string _fundingStreamId;
        private string _fundingPeriodId;
        private IEnumerable<FundingStreamPaymentDate> _paymentDates;

        public FundingStreamPaymentDatesBuilder WithPaymentDates(params FundingStreamPaymentDate[] paymentDates)
        {
            _paymentDates = paymentDates;

            return this;
        }

        public FundingStreamPaymentDatesBuilder WithFundingStreamId(string fundingStreamId)
        {
            _fundingStreamId = fundingStreamId;

            return this;
        }
        
        public FundingStreamPaymentDatesBuilder WithFundingPeriodId(string fundingPeriodId)
        {
            _fundingPeriodId = fundingPeriodId;

            return this;
        }
        
        public FundingStreamPaymentDates Build()
        {
            return new FundingStreamPaymentDates
            {
                FundingPeriodId = _fundingPeriodId ?? NewRandomString(),
                FundingStreamId = _fundingStreamId ?? NewRandomString(),
                PaymentDates = _paymentDates?.ToArray() ?? new FundingStreamPaymentDate[0]
            };
        }
    }
}