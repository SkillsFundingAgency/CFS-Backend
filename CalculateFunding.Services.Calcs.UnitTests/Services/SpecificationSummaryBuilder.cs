using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.Models;
using CalculateFunding.Tests.Common.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CalculateFunding.Services.Calcs.UnitTests.Services
{
    public class SpecificationSummaryBuilder : TestEntityBuilder
    {
        private string _id;
        private string _fundingPeriodId;
        private bool _withNoFundingPeriod;
        private bool? _isSelectedForFunding;
        private IEnumerable<string> _fundingStreamIds = Enumerable.Empty<string>();
        private bool _withNoId;
        private string _providerVersionId;
        private IDictionary<string, string> _templateVersions;

        public SpecificationSummaryBuilder WithTemplateVersions(params (string fundingStream, string templateVersion)[] templateVersions)
        {
            _templateVersions = templateVersions?.ToDictionary(_ => _.fundingStream, _ => _.templateVersion);

            return this;
        }
        
        public SpecificationSummaryBuilder WithNoId()
        {
            _withNoId = true;

            return this;
        }

        public SpecificationSummaryBuilder WithIsSelectedForFunding(bool isSelectedForFunding)
        {
            _isSelectedForFunding = isSelectedForFunding;

            return this;
        }

        public SpecificationSummaryBuilder WithId(string id)
        {
            _id = id;

            return this;
        }

        public SpecificationSummaryBuilder WithProviderVersionId(string id)
        {
            _providerVersionId = id;

            return this;
        }

        public SpecificationSummaryBuilder WithFundingPeriodId(string fundingPeriodId)
        {
            _fundingPeriodId = fundingPeriodId;

            return this;
        }

        public SpecificationSummaryBuilder WithFundingStreamIds(params string[] fundingStreamIds)
        {
            _fundingStreamIds = fundingStreamIds;

            return this;
        }

        public SpecificationSummaryBuilder WithNoFundingPeriod()
        {
            _withNoFundingPeriod = true;

            return this;
        }


        public SpecificationSummary Build()
        {
            return new SpecificationSummary
            {
                Id = _withNoId ? null : _id ?? NewRandomString(),
                TemplateIds = _templateVersions,
                FundingPeriod = _withNoFundingPeriod
                    ? null
                    : new Reference(_fundingPeriodId ?? NewRandomString(), NewRandomString()),
                IsSelectedForFunding = _isSelectedForFunding.GetValueOrDefault(NewRandomFlag()),
                ProviderVersionId = _providerVersionId,
                FundingStreams = _fundingStreamIds.Select(_ => new Reference
                {
                    Id = _
                }).ToArray()
            };
        }
    }
}
