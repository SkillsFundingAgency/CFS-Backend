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

            Models.Specs.Period fundingPeriod = new Models.Specs.Period()
		    {
			    Name = "Name",
			    Id = "Id",
			    StartDate = DateTimeOffset.MinValue,
			    EndDate = DateTimeOffset.MaxValue
		    };

            // Act
            V1.Models.Period mappedPeriod = mapperUnderTest.Map<V1.Models.Period>(fundingPeriod);

			// Assert
		    mappedPeriod.Should().NotBeNull();
		    mappedPeriod.StartDate.Should().Be(fundingPeriod.StartDate);
		    mappedPeriod.EndDate.Should().Be(fundingPeriod.EndDate);
		    mappedPeriod.PeriodId.Should().Be(fundingPeriod.Id);
	    }

        [TestMethod]
        public void Mapper_SpecFundingStreamToModelFundingStream_ShouldReturnCorrectFundingStream()
        {
            // Arrange
            Mapper.Reset();
            MapperConfigurationExpression mappings = new MapperConfigurationExpression();
            mappings.AddProfile<ExternalApiMappingProfile>();
            Mapper.Initialize(mappings);
            IMapper mapperUnderTest = Mapper.Instance;
            string allocationLineId = "Id";
            string allocationLineName = "Name";

            Models.Specs.FundingStream fundingStream = new Models.Specs.FundingStream()
            {
                AllocationLines = new List<Models.Specs.AllocationLine>()
                {
                    new Models.Specs.AllocationLine()
                    {
                        Id = allocationLineId,
                        Name = allocationLineName
                    }
                },
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
                .AllocationLines.Should().Contain(a => a.AllocationLineCode == allocationLineId && a.AllocationLineName == allocationLineName)
                .Should().NotBeNull();
        }

    }
}
