using System;
using System.Linq;
using CalculateFunding.Common.Models;
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
                PublishStatus = _publishStatus.GetValueOrDefault(NewRandomEnum<PublishStatus>())
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