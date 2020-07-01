using System;
using System.Collections.Generic;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Specs;
using CalculateFunding.Models.Versioning;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Specs.UnitTests.MappingProfiles
{
    public class SpecificationSearchModelBuilder : TestEntityBuilder
    {
        private IEnumerable<Reference> _fundingStreams;
        private Reference _fundingPeriod;
        private string _id;
        private string _name;
        private string _description;
        private string _publishStatus;
        private DateTimeOffset? _updatedAt;
        private string[] _dataDefinitionRelationshipIds;
        private bool? _isSelectedForFunding;

        public SpecificationSearchModelBuilder WithId(string id)
        {
            _id = id;

            return this;
        }

        public SpecificationSearchModelBuilder WithName(string name)
        {
            _name = name;

            return this;
        }

        public SpecificationSearchModelBuilder WithDescription(string description)
        {
            _description = description;

            return this;
        }

        public SpecificationSearchModelBuilder WithPublishStatus(string publishStatus)
        {
            _publishStatus = publishStatus;

            return this;
        }

        public SpecificationSearchModelBuilder WithUpdateAt(DateTimeOffset updatedAt)
        {
            _updatedAt = updatedAt;

            return this;
        }

        public SpecificationSearchModelBuilder WithDataDefinitionRelationshipIds(params string[] dataDefinitionRelationshipIds)
        {
            _dataDefinitionRelationshipIds = dataDefinitionRelationshipIds;

            return this;
        }

        public SpecificationSearchModelBuilder WithIsSelectedForFunding(bool isSelectedForFunding)
        {
            _isSelectedForFunding = isSelectedForFunding;

            return this;
        }

        public SpecificationSearchModelBuilder WithFundingPeriod(Reference fundingPeriod)
        {
            _fundingPeriod = fundingPeriod;

            return this;
        }

        public SpecificationSearchModelBuilder WithFundingStreams(params Reference[] fundingStreams)
        {
            _fundingStreams = fundingStreams;

            return this;
        }
        
        public SpecificationSearchModel Build()
        {
            return new SpecificationSearchModel
            {
                Id = _id ?? NewRandomString(),
                Name = _name ?? NewRandomString(),
                Description = _description ?? NewRandomString(),
                PublishStatus = _publishStatus ?? NewRandomEnum<PublishStatus>().ToString(),
                UpdatedAt = _updatedAt.GetValueOrDefault(NewRandomDateTime()),
                DataDefinitionRelationshipIds = _dataDefinitionRelationshipIds,
                IsSelectedForFunding = _isSelectedForFunding.GetValueOrDefault(NewRandomFlag()),
                FundingPeriod = _fundingPeriod,
                FundingStreams   = _fundingStreams,
            };
        }    
    }
}