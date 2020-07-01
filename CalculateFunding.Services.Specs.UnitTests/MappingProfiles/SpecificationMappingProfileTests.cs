using System;
using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Specs;
using CalculateFunding.Services.Specs.MappingProfiles;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.Specs.UnitTests.MappingProfiles
{
    [TestClass]
    public class SpecificationMappingProfileTests
    {
        private MapperConfiguration _configuration;
        private Mapper _mapper;

        [TestInitialize]
        public void SetUp()
        {
            _configuration = new MapperConfiguration(configure => configure.AddProfile<SpecificationsMappingProfile>());     
            _mapper = new Mapper(_configuration);
        }

        [TestMethod]
        public void SpecificationsMappingProfile_ShouldBeValid()
        {
            Action action = () => _configuration.AssertConfigurationIsValid();

            action
                .Should()
                .NotThrow("Mapping configuration should be valid for SpecificationsMappingProfile");
        }

        [TestMethod]
        public void SpecificationSearchModelToSpecificationIndexMapping()
        {
            SpecificationSearchModel specificationSearchModel = NewSpecificationSearchModel(_ => _.WithFundingPeriod(NewFundingPeriod())
                .WithDataDefinitionRelationshipIds(NewRandomString(), NewRandomString())
                .WithFundingStreams(NewFundingStream(), NewFundingStream()));
            
            
            SpecificationIndex searchIndex = WhenTheSourceItemsAreMapped(specificationSearchModel).Single();
            
            searchIndex
                .Should()
                .BeEquivalentTo(new SpecificationIndex
                {
                    Id = specificationSearchModel.Id,
                    Name = specificationSearchModel.Name,
                    FundingPeriodId = specificationSearchModel.FundingPeriod.Id,
                    FundingPeriodName = specificationSearchModel.FundingPeriod.Name,
                    FundingStreamIds = specificationSearchModel.FundingStreams.Select(_ => _.Id).ToArray(),
                    FundingStreamNames = specificationSearchModel.FundingStreams.Select(_ => _.Name).ToArray(),
                    Description = specificationSearchModel.Description,
                    LastUpdatedDate = specificationSearchModel.UpdatedAt,
                    DataDefinitionRelationshipIds = specificationSearchModel.DataDefinitionRelationshipIds.ToArray(),
                    Status = specificationSearchModel.PublishStatus,
                    IsSelectedForFunding = specificationSearchModel.IsSelectedForFunding
                }, opt => opt.ComparingByMembers<Reference>());

        }
        
        [TestMethod]
        public void SpecificationToSpecificationIndexMapping()
        {
            Specification specification = NewSpecification(_ => _.WithCurrent(NewSpecificationVersion(curr => 
                curr.WithFundingStreamsIds(NewRandomString(), NewRandomString())
                    .WithDataDefinitionRelationshipIds(NewRandomString(), NewRandomString(), NewRandomString()))));

            SpecificationIndex searchIndex = WhenTheSourceItemsAreMapped(specification).Single();
            
            searchIndex
                .Should()
                .BeEquivalentTo(new SpecificationIndex
                {
                    Id = specification.Id,
                    Name = specification.Name,
                    FundingPeriodId = specification.Current.FundingPeriod.Id,
                    FundingPeriodName = specification.Current.FundingPeriod.Name,
                    FundingStreamIds = specification.Current.FundingStreams.Select(_ => _.Id).ToArray(),
                    FundingStreamNames = specification.Current.FundingStreams.Select(_ => _.Name).ToArray(),
                    Description = specification.Current.Description,
                    LastUpdatedDate = specification.Current.Date,
                    IsSelectedForFunding = specification.IsSelectedForFunding,
                    DataDefinitionRelationshipIds = specification.Current.DataDefinitionRelationshipIds.ToArray(),
                    Status = specification.Current.PublishStatus.ToString()
                }, opt => opt.ComparingByMembers<Reference>());
        }
        
        private string NewRandomString() => new RandomString();

        private IEnumerable<SpecificationIndex> WhenTheSourceItemsAreMapped<TSource>(params TSource[] sourceItems)
            => _mapper.Map<IEnumerable<SpecificationIndex>>(sourceItems);

        private Reference NewFundingStream(Action<ReferenceBuilder> setUp = null)
            => NewReference(setUp);
        
        private Reference NewFundingPeriod(Action<ReferenceBuilder> setUp = null)
            => NewReference(setUp);

        private Reference NewReference(Action<ReferenceBuilder> setUp = null)
        {
            ReferenceBuilder referenceBuilder = new ReferenceBuilder();

            setUp?.Invoke(referenceBuilder);
            
            return referenceBuilder.Build();
        }
        
        private SpecificationSearchModel NewSpecificationSearchModel(Action<SpecificationSearchModelBuilder> setUp = null)
        {
            SpecificationSearchModelBuilder specificationSearchModelBuilder = new SpecificationSearchModelBuilder();

            setUp?.Invoke(specificationSearchModelBuilder);
            
            return specificationSearchModelBuilder.Build();
        }

        private Specification NewSpecification(Action<SpecificationBuilder> setUp = null)
        {
            SpecificationBuilder specificationBuilder = new SpecificationBuilder();

            setUp?.Invoke(specificationBuilder);
            
            return specificationBuilder.Build();
        }

        private SpecificationVersion NewSpecificationVersion(Action<SpecificationVersionBuilder> setUp = null)
        {
            SpecificationVersionBuilder specificationVersionBuilder = new SpecificationVersionBuilder();

            setUp?.Invoke(specificationVersionBuilder);
            
            return specificationVersionBuilder.Build();
        }
    }
}
