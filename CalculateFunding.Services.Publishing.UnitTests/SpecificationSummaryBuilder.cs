﻿using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Policies.Models;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.Models;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Publishing.UnitTests
{
    public class SpecificationSummaryBuilder : TestEntityBuilder
    {
        private string _id;
        private string _fundingPeriodId;
        private bool _withNoFundingPeriod;
        private bool? _isSelectedForFunding;
        private IEnumerable<string> _fundingStreamIds = Enumerable.Empty<string>();
        private bool _withNoId;
        private IEnumerable<(string fundingId, string version)> _templateIds = Enumerable.Empty<(string fundingId, string version)>();
        private string _providerVersionId;
        private PublishStatus _publishStatus;

        public SpecificationSummaryBuilder WithNoId()
        {
            _withNoId = true;

            return this;
        }

        public SpecificationSummaryBuilder WithPublishStatus(PublishStatus publishStatus)
        {
            _publishStatus = publishStatus;

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

        public SpecificationSummaryBuilder WithTemplateIds(params (string fundingId, string version)[] ids)
        {
            _templateIds = ids;

            return this;
        }

        public SpecificationSummaryBuilder WithProviderVersionId(string providerVersionId)
        {
            _providerVersionId = providerVersionId;

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
                FundingStreams = _fundingStreamIds.Select(_ => new FundingStream
                {
                    Id = _
                }).ToArray(),
                TemplateIds = _templateIds.ToDictionary(_ => _.fundingId, _ => _.version),
                ProviderVersionId = _providerVersionId ?? NewRandomString(),
                ApprovalStatus = _publishStatus
            };
        }
    }
}