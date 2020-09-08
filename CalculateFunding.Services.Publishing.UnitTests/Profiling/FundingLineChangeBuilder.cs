using CalculateFunding.Common.Models;
using CalculateFunding.Services.Publishing.Profiling;
using CalculateFunding.Tests.Common.Helpers;
using System;
using System.Collections.Generic;

namespace CalculateFunding.Services.Publishing.UnitTests.Profiling
{
    public class FundingLineChangeBuilder : TestEntityBuilder
    {
        private decimal? _carryOverAmount;
        private decimal? _fundingLineTotal;
        private decimal? _previousFundingLineTotal;
        private IEnumerable<ProfileTotal> _profileTotals;
        private string _fundingLineName;
        private string _fundingStreamName;
        private DateTime? _lastUpdatedDate;
        private Reference _lastUpdatedUser;

        public FundingLineChangeBuilder WithFundingStreamName(string fundingStreamName)
        {
            _fundingStreamName = fundingStreamName;

            return this;
        }

        public FundingLineChangeBuilder WithFundingLineName(string fundingLineName)
        {
            _fundingLineName = fundingLineName;

            return this;
        }

        public FundingLineChangeBuilder WithCarryOverAmount(decimal carryOverAmount)
        {
            _carryOverAmount = carryOverAmount;

            return this;
        }

        public FundingLineChangeBuilder WithFundingLineTotal(decimal fundingLineTotal)
        {
            _fundingLineTotal = fundingLineTotal;

            return this;
        }

        public FundingLineChangeBuilder WithPreviousFundingLineTotal(decimal previousFundingLineTotal)
        {
            _previousFundingLineTotal = previousFundingLineTotal;

            return this;
        }

        public FundingLineChangeBuilder WithProfileTotals(IEnumerable<ProfileTotal> profileTotals)
        {
            _profileTotals = profileTotals;

            return this;
        }

        public FundingLineChangeBuilder WithLastUpdatedDate(DateTime lastUpdatedDate)
        {
            _lastUpdatedDate = lastUpdatedDate;

            return this;
        }

        public FundingLineChangeBuilder WithLastUpdatedUser(Reference lastUpdatedUser)
        {
            _lastUpdatedUser = lastUpdatedUser;

            return this;
        }

        public FundingLineChange Build()
        {
            return new FundingLineChange
            {
                CarryOverAmount = _carryOverAmount.GetValueOrDefault(NewRandomNumberBetween(1000, 99999)),
                FundingLineTotal = _fundingLineTotal.GetValueOrDefault(NewRandomNumberBetween(1000, 99999)),
                PreviousFundingLineTotal = _previousFundingLineTotal.GetValueOrDefault(NewRandomNumberBetween(1000, 99999)),
                ProfileTotals = _profileTotals,
                FundingLineName = _fundingLineName,
                FundingStreamName = _fundingStreamName,
                LastUpdatedDate = _lastUpdatedDate.GetValueOrDefault(NewRandomDateTime().DateTime),
                LastUpdatedUser = _lastUpdatedUser
            };
        }

    }
}
