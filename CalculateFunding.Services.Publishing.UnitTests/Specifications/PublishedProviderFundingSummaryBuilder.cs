using System;
using CalculateFunding.Services.Publishing.Models;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Publishing.UnitTests.Specifications
{
    public class PublishedProviderFundingSummaryBuilder : TestEntityBuilder
    {
        private string _specificationId;
        private string _providerId;
        private string _providerSubType;
        private string _providerType;
        private decimal? _totalFunding;
        private int _majorVersion;
        private int _minorVersion;
        private bool _isIndicative;
        private string _channelCode;
        private string _channelName;

        public PublishedProviderFundingSummaryBuilder WithSpecificationId(string specificationId)
        {
            _specificationId = specificationId;
            return this;
        }

        public PublishedProviderFundingSummaryBuilder WithChannelCode(string channelCode)
        {
            _channelCode = channelCode;
            return this;
        }

        public PublishedProviderFundingSummaryBuilder WithChannelName(string channelName)
        {
            _channelName = channelName;
            return this;
        }

        public PublishedProviderFundingSummaryBuilder WithProviderId(string providerId)
        {
            _providerId = providerId;
            return this;
        }

        public PublishedProviderFundingSummaryBuilder WithProviderType(string providerType)
        {
            _providerType = providerType;
            return this;
        }

        public PublishedProviderFundingSummaryBuilder WithProviderSubType(string providerSubType)
        {
            _providerSubType = providerSubType;
            return this;
        }

        public PublishedProviderFundingSummaryBuilder WithTotalFunding(decimal totalFunding)
        {
            _totalFunding = totalFunding;
            return this;
        }

        public PublishedProviderFundingSummaryBuilder WithMajorVersion(int majorVersion)
        {
            _majorVersion = majorVersion;
            return this;
        }

        public PublishedProviderFundingSummaryBuilder WithMinorVersion(int minorVersion)
        {
            _minorVersion = minorVersion;
            return this;
        }

        public PublishedProviderFundingSummaryBuilder WithIsIndicative(bool isIndicative)
        {
            _isIndicative = isIndicative;
            return this;
        }

        public PublishedProviderFundingSummary Build()
        {
            return new PublishedProviderFundingSummary
            {
                SpecificationId = _specificationId ?? NewRandomString(),
                ProviderId = _providerId ?? NewRandomString(),
                ProviderType = _providerType ?? NewRandomString(),
                ProviderSubType = _providerSubType ?? NewRandomString(),
                TotalFunding = _totalFunding ?? NewRandomNumberBetween(10000, Int32.MaxValue),
                MajorVersion = _majorVersion,
                MinorVersion = _minorVersion,
                IsIndicative = _isIndicative,
                ChannelCode = _channelCode ?? NewRandomString(),
                ChannelName = _channelName ?? NewRandomString()
            };
        }
    }
}