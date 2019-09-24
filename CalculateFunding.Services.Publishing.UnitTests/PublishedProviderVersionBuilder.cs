using System.Collections.Generic;
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
                Provider = _provider
            };
        }
    }
}
