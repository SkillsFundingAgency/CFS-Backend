using System;
using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Providers;
using CalculateFunding.Models.Specs;
using CalculateFunding.Models.Versioning;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Specs.UnitTests
{
    public class SpecificationVersionBuilder : TestEntityBuilder
    {
        private string _specificationId;
        private string _description;
        private string[] _fundingStreamIds = new string[0];
        private string _fundingPeriodId;
        private string _fundingPeriodName;
        private DateTimeOffset? _date;
        private string[] _dataDefinitionRelationshipIds;
        private PublishStatus? _publishStatus;
        private Dictionary<string, string> _templateIds;
        private int? _providerSnapshotId;
        private ProviderSource? _providerSource;
        private string _providerVersionId;

        public SpecificationVersionBuilder WithTemplateIds(params (string fundingStreamId, string templateId)[] assignedTemplateIds)
        {
            _templateIds = assignedTemplateIds
                .ToDictionary(_ => _.fundingStreamId, _ => _.templateId);

            return this;
        }

        public SpecificationVersionBuilder WithPublishStatus(PublishStatus publishStatus)
        {
            _publishStatus = publishStatus;

            return this;
        }
        
        public SpecificationVersionBuilder WithDataDefinitionRelationshipIds(params string[] dataDefinitionRelationshipIds)
        {
            _dataDefinitionRelationshipIds = dataDefinitionRelationshipIds;

            return this;
        }

        public SpecificationVersionBuilder WithSpecificationId(string id)
        {
            _specificationId = id;

            return this;
        }
        
        public SpecificationVersionBuilder WithDate(DateTimeOffset date)
        {
            _date = date;

            return this;
        }

        
        public SpecificationVersionBuilder WithDescription(string description)
        {
            _description = description;

            return this;
        }

        public SpecificationVersionBuilder WithFundingStreamsIds(params string[] fundingStreamIds)
        {
            _fundingStreamIds = fundingStreamIds;

            return this;
        }

        public SpecificationVersionBuilder WithFundingPeriodId(string fundingPeriodId)
        {
            _fundingPeriodId = fundingPeriodId;

            return this;
        }
        
        public SpecificationVersionBuilder WithFundingPeriodName(string fundingPeriodName)
        {
            _fundingPeriodName = fundingPeriodName;

            return this;
        }

        public SpecificationVersionBuilder WithProviderSnapshotId(int? providerSnapshotId)
        {
            _providerSnapshotId = providerSnapshotId;

            return this;
        }

        public SpecificationVersionBuilder WithProviderVersionId(string providerVersionId)
        {
            _providerVersionId = providerVersionId;

            return this;
        }

        public SpecificationVersionBuilder WithProviderSource(ProviderSource providerSource)
        {
            _providerSource = providerSource;

            return this;
        }

        public SpecificationVersion Build()
        {
            return new SpecificationVersion
            {
                Description = _description ?? NewRandomString(),
                Date = _date.GetValueOrDefault(NewRandomDateTime()),
                DataDefinitionRelationshipIds = _dataDefinitionRelationshipIds,
                SpecificationId = _specificationId,
                FundingPeriod = NewReferenceForId(_fundingPeriodId, _fundingPeriodName),
                FundingStreams = _fundingStreamIds.Select(id => NewReferenceForId(id)).ToArray(),
                PublishStatus = _publishStatus.GetValueOrDefault(NewRandomEnum<PublishStatus>()),
                TemplateIds = _templateIds ?? new Dictionary<string, string>(),
                ProviderSnapshotId = _providerSnapshotId ?? NewRandomNumberBetween(1, 10),
                ProviderSource = _providerSource.GetValueOrDefault(NewRandomEnum(ProviderSource.CFS)),
                ProviderVersionId = _providerVersionId
            };
        }

        private Reference NewReferenceForId(string id, string name = null)
        {
            return new Reference
            {
                Id = id ?? NewRandomString(),
                Name = name ?? NewRandomString()
            };
        }
    }
}