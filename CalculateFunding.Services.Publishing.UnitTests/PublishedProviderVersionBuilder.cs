using CalculateFunding.Models.Publishing;
using System;
using System.Collections.Generic;
using System.Text;
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

        public PublishedProviderVersionBuilder WithSpecificationId(string specificationId)
        {
            _specificationId = specificationId;

            return this;
        }

        public PublishedProviderVersionBuilder WithDefaults(string providerId = null, string fundingPeriodId = null, string fundingStreamId = null, int? version = null)
        {
            _providerId = providerId;
            _fundingPeriodId = fundingPeriodId;
            _fundingStreamId = fundingStreamId;
            _version = version;
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
                Version = _version ?? 1
            };
        }
    }
}
