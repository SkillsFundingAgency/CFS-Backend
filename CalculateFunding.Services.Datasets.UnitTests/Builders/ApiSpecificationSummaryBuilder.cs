using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.Models;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Datasets.Builders
{
    public class ApiSpecificationSummaryBuilder : TestEntityBuilder
    {
        private string _id;
        private string _fundingPeriodId;
        private bool _withNoFundingPeriod;
        private bool? _isSelectedForFunding;
        private IEnumerable<string> _fundingStreamIds = Enumerable.Empty<string>();
        private bool _withNoId;
        private string _providerVersionId;
        private ProviderSource? _providerSource;

        public ApiSpecificationSummaryBuilder WithNoId()
        {
            _withNoId = true;

            return this;
        }

        public ApiSpecificationSummaryBuilder WithIsSelectedForFunding(bool isSelectedForFunding)
        {
            _isSelectedForFunding = isSelectedForFunding;

            return this;
        }

        public ApiSpecificationSummaryBuilder WithId(string id)
        {
            _id = id;

            return this;
        }

        public ApiSpecificationSummaryBuilder WithProviderVersionId(string id)
        {
            _providerVersionId = id;

            return this;
        }

        public ApiSpecificationSummaryBuilder WithFundingPeriodId(string fundingPeriodId)
        {
            _fundingPeriodId = fundingPeriodId;

            return this;
        }

        public ApiSpecificationSummaryBuilder WithFundingStreamIds(params string[] fundingStreamIds)
        {
            _fundingStreamIds = fundingStreamIds;

            return this;
        }

        public ApiSpecificationSummaryBuilder WithNoFundingPeriod()
        {
            _withNoFundingPeriod = true;

            return this;
        }

        public ApiSpecificationSummaryBuilder WithProviderSource(ProviderSource providerSource)
        {
            _providerSource = providerSource;
            return this;
        }

        public SpecificationSummary Build()
        {
            return new SpecificationSummary
            {
                Id = _withNoId ? null : _id ?? NewRandomString(),
                FundingPeriod = _withNoFundingPeriod
                    ? null
                    : new Reference(_fundingPeriodId ?? NewRandomString(), NewRandomString()),
                IsSelectedForFunding = _isSelectedForFunding.GetValueOrDefault(NewRandomFlag()),
                ProviderVersionId = _providerVersionId,
                FundingStreams = _fundingStreamIds.Select(_ => new Reference
                {
                    Id = _
                }).ToArray(),
                ProviderSource = _providerSource ?? ProviderSource.CFS
            };
        }
    }
}
