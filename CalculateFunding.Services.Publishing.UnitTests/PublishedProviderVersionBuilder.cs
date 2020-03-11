using System;
using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Publishing.UnitTests
{
    public class PublishedProviderVersionBuilder : TestEntityBuilder
    {
        private string _providerId;
        private string _fundingPeriodId;
        private string _fundingStreamId;
        private int? _version;
        private string _specificationId;
        private PublishedProviderStatus? _status;
        private Provider _provider;
        private int? _majorVersion;
        private int _minorVersion;
        private IEnumerable<FundingLine> _fundingLines;
        private IEnumerable<FundingCalculation> _calculations;
        private decimal? _totalFunding;
        private IEnumerable<string> _predecessors;
        private Reference _author;
        private DateTimeOffset? _date;
        private IEnumerable<VariationReason> _variationReasons;

        public PublishedProviderVersionBuilder WithVariationReasons(IEnumerable<VariationReason> variationReasons)
        {
            _variationReasons = variationReasons;

            return this;
        }

        public PublishedProviderVersionBuilder WithDate(string dateLiteral)
        {
            _date = DateTimeOffset.Parse(dateLiteral);

            return this;
        }

        public PublishedProviderVersionBuilder WithAuthor(Reference author)
        {
            _author = author;

            return this;
        }

        public PublishedProviderVersionBuilder WithPredecessors(params string[] predecessors)
        {
            _predecessors = predecessors;

            return this;
        }
        
        public PublishedProviderVersionBuilder WithTotalFunding(decimal totalFunding)
        {
            _totalFunding = totalFunding;

            return this;
        }

        public PublishedProviderVersionBuilder WithProvider(Provider provider)
        {
            _provider = provider;

            return this;
        }

        public PublishedProviderVersionBuilder WithPublishedProviderStatus(PublishedProviderStatus status)
        {
            _status = status;

            return this;
        }
        
        public PublishedProviderVersionBuilder WithSpecificationId(string specificationId)
        {
            _specificationId = specificationId;

            return this;
        }

        public PublishedProviderVersionBuilder WithFundingStreamId(string fundingStreamId)
        {
            _fundingStreamId = fundingStreamId;

            return this;
        }

        public PublishedProviderVersionBuilder WithFundingPeriodId(string fundingPeriodId)
        {
            _fundingPeriodId = fundingPeriodId;

            return this;
        }

        public PublishedProviderVersionBuilder WithProviderId(string providerId)
        {
            _providerId = providerId;

            return this;
        }

        public PublishedProviderVersionBuilder WithVersion(int version)
        {
            _version = version;

            return this;
        }

        public PublishedProviderVersionBuilder WithMajorVersion(int majorVersion)
        {
            _majorVersion = majorVersion;

            return this;
        }

        public PublishedProviderVersionBuilder WithMinorVersion(int minorVersion)
        {
            _minorVersion = minorVersion;

            return this;
        }

        public PublishedProviderVersionBuilder WithFundingLines(params FundingLine[] fundingLines)
        {
            _fundingLines = fundingLines;

            return this;
        }

        public PublishedProviderVersionBuilder WithFundingCalculations(params FundingCalculation[] calculations)
        {
            _calculations = calculations;

            return this;
        }

        public PublishedProviderVersion Build()
        {
            return new PublishedProviderVersion
            {
                Author = _author,
                SpecificationId = _specificationId ?? NewRandomString(),
                ProviderId = _providerId ?? NewRandomString(),
                FundingPeriodId = _fundingPeriodId ?? NewRandomString(),
                FundingStreamId = _fundingStreamId ?? NewRandomString(),
                Version = _version ?? 1,
                MajorVersion = _majorVersion ?? 1,
                MinorVersion = _minorVersion,
                Status = _status.GetValueOrDefault(NewRandomEnum<PublishedProviderStatus>()),
                FundingLines = _fundingLines,
                Calculations = _calculations,
                Provider = _provider,
                TotalFunding = _totalFunding,
                Predecessors = _predecessors?.ToList(),
                Date = _date.GetValueOrDefault(NewRandomDateTime()),
                VariationReasons = _variationReasons
            };
        }
    }
}
