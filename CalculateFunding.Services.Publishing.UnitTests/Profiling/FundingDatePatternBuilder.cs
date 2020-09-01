using CalculateFunding.Common.ApiClient.Policies;
using CalculateFunding.Tests.Common.Helpers;
using System;

namespace CalculateFunding.Services.Publishing.UnitTests.Profiling
{
    public class FundingDatePatternBuilder : TestEntityBuilder
    {
        private int _occurrence;
        private DateTimeOffset _paymentDate;
        private string _period;
        private int _periodYear;

        public FundingDatePatternBuilder WithOccurrence(int occurrence)
        {
            _occurrence = occurrence;

            return this;
        }

        public FundingDatePatternBuilder WithPaymentDate(DateTimeOffset paymentDate)
        {
            _paymentDate = paymentDate;

            return this;
        }

        public FundingDatePatternBuilder WithPeriod(string period)
        {
            _period = period;

            return this;
        }

        public FundingDatePatternBuilder WithPeriodYear(int periodYear)
        {
            _periodYear = periodYear;

            return this;
        }

        public FundingDatePattern Build()
        {
            return new FundingDatePattern
            {
                Occurrence = _occurrence,
                PaymentDate = _paymentDate,
                Period = _period,
                PeriodYear = _periodYear
            };
        }
    }
}
