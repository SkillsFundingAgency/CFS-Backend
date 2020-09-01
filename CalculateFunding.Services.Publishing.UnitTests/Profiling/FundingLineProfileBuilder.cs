using CalculateFunding.Common.Models;
using CalculateFunding.Services.Publishing.Profiling;
using CalculateFunding.Tests.Common.Helpers;
using System;
using System.Collections.Generic;

namespace CalculateFunding.Services.Publishing.UnitTests.Profiling
{
    public class FundingLineProfileBuilder : TestEntityBuilder
    {
        private decimal? _amountAlreadyPaid;
        private decimal? _carryOverAmount;
        private DateTime? _lastUpdatedDate;
        private Reference _lastUpdatedUser;
        private string _profilePatternKey;
        private string _providerName;
        private decimal? _remainingAmount;
        private decimal? _totalAllocation;
        private IEnumerable<ProfileTotal> _profileTotals;
        private decimal? _profileTotalAmount;

        public FundingLineProfileBuilder WithProfileTotalAmount(decimal profileTotalAmount)
        {
            _profileTotalAmount = profileTotalAmount;

            return this;
        }

        public FundingLineProfileBuilder WithAmountAlreadyPaid(decimal amountAlreadyPaid)
        {
            _amountAlreadyPaid = amountAlreadyPaid;

            return this;
        }

        public FundingLineProfileBuilder WithCarryOverAmount(decimal carryOverAmount)
        {
            _carryOverAmount = carryOverAmount;

            return this;
        }

        public FundingLineProfileBuilder WithLastUpdatedDate(DateTime lastUpdatedDate)
        {
            _lastUpdatedDate = lastUpdatedDate;

            return this;
        }

        public FundingLineProfileBuilder WithLastUpdatedUser(Reference lastUpdatedUser)
        {
            _lastUpdatedUser = lastUpdatedUser;

            return this;
        }

        public FundingLineProfileBuilder WithProfilePatternKey(string profilePatternKey)
        {
            _profilePatternKey = profilePatternKey;

            return this;
        }

        public FundingLineProfileBuilder WithProviderName(string providerName)
        {
            _providerName = providerName;

            return this;
        }

        public FundingLineProfileBuilder WithRemainingAmount(decimal remainingAmount)
        {
            _remainingAmount = remainingAmount;

            return this;
        }

        public FundingLineProfileBuilder WithTotalAllocation(decimal totalAllocation)
        {
            _totalAllocation = totalAllocation;

            return this;
        }

        public FundingLineProfileBuilder WithProfileTotals(IEnumerable<ProfileTotal> profileTotals)
        {
            _profileTotals = profileTotals;

            return this;
        }

        public FundingLineProfile Build()
        {
            return new FundingLineProfile
            {
                AmountAlreadyPaid = _amountAlreadyPaid.GetValueOrDefault(NewRandomNumberBetween(1000, 99999)),
                CarryOverAmount = _carryOverAmount.GetValueOrDefault(NewRandomNumberBetween(1000, 99999)),
                LastUpdatedDate = _lastUpdatedDate.GetValueOrDefault(NewRandomDateTime().DateTime),
                LastUpdatedUser = _lastUpdatedUser,
                ProfilePatternKey = _profilePatternKey,
                ProviderName = _providerName,
                RemainingAmount = _remainingAmount.GetValueOrDefault(NewRandomNumberBetween(1000, 99999)),
                TotalAllocation = _totalAllocation.GetValueOrDefault(NewRandomNumberBetween(1000, 99999)),
                ProfileTotals = _profileTotals,
                ProfileTotalAmount = _profileTotalAmount.GetValueOrDefault(NewRandomNumberBetween(1000, 99999)),
            };
        }
    }
}
