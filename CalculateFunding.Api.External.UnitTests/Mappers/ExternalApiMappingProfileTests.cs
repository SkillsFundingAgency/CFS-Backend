using System;
using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using AutoMapper.Configuration;
using CalculateFunding.Api.External.MappingProfiles;
using CalculateFunding.Api.External.V1.Models;
using CalculateFunding.Models.Specs;
using CalculateFunding.Tests.Common;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Api.External.UnitTests.Mappers
{
	[TestClass]
    public class ExternalApiMappingProfileTests
    {
	    [TestMethod]
	    public void Mapper_IsConfigurationValid()
	    {
		    // Arrange
		    MapperConfiguration config = new MapperConfiguration(c => c.AddProfile<MappingProfiles.ExternalApiMappingProfile>());

		    //Act
			Action action = () =>
		    {
			    config.AssertConfigurationIsValid();
		    };

		    //Assert
		    action.Should().NotThrow();
		}

	    [TestMethod]
	    public void Mapper_FundingPeriodToPeriod_ShouldReturnCorrectPeriod()
	    {
			// Arrange
			Mapper.Reset();
		    MapperConfigurationExpression mappings = new MapperConfigurationExpression();
			mappings.AddProfile<ExternalApiMappingProfile>();
			Mapper.Initialize(mappings);
		    IMapper mapperUnderTest = Mapper.Instance;

		    FundingPeriod fundingPeriod = new FundingPeriod()
		    {
			    Name = "Name",
			    Type = "Type",
			    Id = "Id",
			    StartDate = DateTimeOffset.MinValue,
			    EndDate = DateTimeOffset.MaxValue
		    };

			// Act
		    Period mappedPeriod = mapperUnderTest.Map<Period>(fundingPeriod);

			// Assert
		    mappedPeriod.Should().NotBeNull();
		    mappedPeriod.StartDate.Should().Be(fundingPeriod.StartDate);
		    mappedPeriod.EndDate.Should().Be(fundingPeriod.EndDate);
		    mappedPeriod.PeriodType.Should().Be(fundingPeriod.Type);
		    mappedPeriod.PeriodId.Should().Be(fundingPeriod.Id);
	    }

        [TestMethod]
        public void Mapper_SpecFundingStreamToModelFundingStream_ShouldReturnCorrectPeriod()
        {
            // Arrange
            Mapper.Reset();
            MapperConfigurationExpression mappings = new MapperConfigurationExpression();
            mappings.AddProfile<ExternalApiMappingProfile>();
            Mapper.Initialize(mappings);
            IMapper mapperUnderTest = Mapper.Instance;
            
            Models.Specs.AllocationLine allocation = new Models.Specs.AllocationLine()
            {
                Id = "Id",
                Name = "Name"
            };

            List<Models.Specs.AllocationLine> allocationLineList = new List<Models.Specs.AllocationLine>();
            allocationLineList.Add(allocation);

            Models.Specs.FundingStream fundingStream = new Models.Specs.FundingStream()
            {
                AllocationLines = allocationLineList,
                Name = "Name",
                Id = "id"
            };

            // Act
            V1.Models.FundingStream mappedFundingStream = mapperUnderTest.Map<V1.Models.FundingStream>(fundingStream);

            // Assert
            mappedFundingStream.Should().NotBeNull();
            mappedFundingStream.FundingStreamCode.Should().Be(fundingStream.Id);
            mappedFundingStream.FundingStreamName.Should().Be(fundingStream.Name);
            mappedFundingStream
                .AllocationLines
                .Single(a => a.AllocationLineCode == allocation.Id && a.AllocationLineName == allocation.Name)
                .Should().NotBeNull();
        }

    }
}
