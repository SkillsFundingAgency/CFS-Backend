﻿using System;
using System.Collections.Generic;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Profiling;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Publishing.UnitTests.Profiling
{
    public class FundingLineProfileBuilder : TestEntityBuilder
    {
        private decimal? _amountAlreadyPaid;
        private decimal? _carryOverAmount;
        private DateTime? _lastUpdatedDate;
        private Reference _lastUpdatedUser;
        private string _profilePatternKey;
        private string _profilePatternName;
        private string _profilePatternDescription;
        private string _fundingLineCode;
        private string _fundingLineName;
        private string _providerName;
        private string _providerId;
        private string _UKPRN;
        private decimal? _remainingAmount;
        private decimal? _profilePatternTotal;
        private IEnumerable<ProfileTotal> _profileTotals;
        private IEnumerable<PublishedProviderError> _errors;
        private decimal? _profileTotalAmount;
        private decimal? _fundingLineAmount;
        private bool _isCustomProfile;
        private decimal? _profilePatternTotalWithCarryOver;

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

        public FundingLineProfileBuilder WithProfilePatternName(string profilePatternName)
        {
            _profilePatternName = profilePatternName;

            return this;
        }

        public FundingLineProfileBuilder WithProfilePatternDescription(string profilePatternDescription)
        {
            _profilePatternDescription = profilePatternDescription;

            return this;
        }

        public FundingLineProfileBuilder WithFundingLineCode(string fundingLineCode)
        {
            _fundingLineCode = fundingLineCode;

            return this;
        }

        public FundingLineProfileBuilder WithFundingLineName(string fundingLineName)
        {
            _fundingLineName = fundingLineName;

            return this;
        }

        public FundingLineProfileBuilder WithFundingLineAmount(decimal? fundingLineAmount)
        {
            _fundingLineAmount = fundingLineAmount;

            return this;
        }

        public FundingLineProfileBuilder WithProviderName(string providerName)
        {
            _providerName = providerName;

            return this;
        }

        public FundingLineProfileBuilder WithProviderId(string providerId)
        {
            _providerId = providerId;

            return this;
        }

        public FundingLineProfileBuilder WithUKPRN(string UKPRN)
        {
            _UKPRN = UKPRN;

            return this;
        }

        public FundingLineProfileBuilder WithRemainingAmount(decimal remainingAmount)
        {
            _remainingAmount = remainingAmount;

            return this;
        }

        public FundingLineProfileBuilder WithProfilePatternTotal(decimal profilePatternTotal)
        {
            _profilePatternTotal = profilePatternTotal;

            return this;
        }

        public FundingLineProfileBuilder WithProfileTotals(IEnumerable<ProfileTotal> profileTotals)
        {
            _profileTotals = profileTotals;

            return this;
        }

        public FundingLineProfileBuilder WithErrors(params PublishedProviderError[] errors)
        {
            _errors = errors;

            return this;
        }

        public FundingLineProfileBuilder WithCustomProfile(bool isCustomProfile)
        {
            _isCustomProfile = isCustomProfile;

            return this;
        }

        public FundingLineProfileBuilder WithProfilePatternTotalWithCarryOver(decimal? profilePatternTotalWithCarryOver)
        {
            _profilePatternTotalWithCarryOver = profilePatternTotalWithCarryOver;

            return this;
        }

        public FundingLineProfile Build()
        {
            return new FundingLineProfile
            {
                FundingLineCode = _fundingLineCode,
                FundingLineName = _fundingLineName,
                ProfilePatternName = _profilePatternName,
                ProfilePatternDescription = _profilePatternDescription,
                AmountAlreadyPaid = _amountAlreadyPaid.GetValueOrDefault(),
                CarryOverAmount = _carryOverAmount.GetValueOrDefault(NewRandomNumberBetween(1000, 99999)),
                LastUpdatedDate = _lastUpdatedDate.GetValueOrDefault(NewRandomDateTime().DateTime),
                LastUpdatedUser = _lastUpdatedUser,
                ProfilePatternKey = _profilePatternKey,
                ProviderName = _providerName,
                ProviderId = _providerId,
                UKPRN = _UKPRN,
                RemainingAmount = _remainingAmount.GetValueOrDefault(),
                ProfilePatternTotal = _profilePatternTotal.GetValueOrDefault(),
                ProfileTotals = _profileTotals,
                IsCustomProfile = _isCustomProfile,
                ProfilePatternTotalWithCarryOver = _profilePatternTotalWithCarryOver,
                Errors = _errors,
                FundingLineAmount = _fundingLineAmount,
            };
        }
    }
}
