using System.Collections.Generic;
using CalculateFunding.Common.ApiClient.Profiling.Models;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Publishing.UnitTests.Profiling
{
    public class ReProfileRequestTestEntityBuilder : TestEntityBuilder
    {
        private IEnumerable<ExistingProfilePeriod> _existingProfilePeriods;
        private decimal? _fundingLineTotal;
        private decimal? _existingFundingLineTotal;

        public ReProfileRequestTestEntityBuilder WithExistingProfilePeriods(params ExistingProfilePeriod[] existingProfilePeriods)
        {
            _existingProfilePeriods = existingProfilePeriods;

            return this;
        }

        public ReProfileRequestTestEntityBuilder WithFundingLineTotal(decimal fundingLineTotal)
        {
            _fundingLineTotal = fundingLineTotal;

            return this;
        }
        public ReProfileRequestTestEntityBuilder WithExistingFundingLineTotal(decimal existingFundingLineTotal)
        {
            _existingFundingLineTotal = existingFundingLineTotal;

            return this;
        }
        
        public ReProfileRequest Build()
        {
            return new ReProfileRequest
            {
                ExistingFundingLineTotal = _existingFundingLineTotal.GetValueOrDefault(NewRandomNumberBetween(999, int.MaxValue)),
                FundingLineTotal = _existingFundingLineTotal.GetValueOrDefault(NewRandomNumberBetween(999, int.MaxValue)),
                ExistingPeriods  = _existingProfilePeriods,
            };
        }
    }
}