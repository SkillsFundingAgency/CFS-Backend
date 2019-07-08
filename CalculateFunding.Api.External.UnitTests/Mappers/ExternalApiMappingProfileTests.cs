using System;
using System.Collections.Generic;
using AutoMapper;
using AutoMapper.Configuration;
using CalculateFunding.Api.External.MappingProfiles;
using FluentAssertions;
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

            Models.Policy.Period fundingPeriod = new Models.Policy.Period()
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
		    mappedPeriod.StartYear.Should().Be(fundingPeriod.StartYear);
		    mappedPeriod.EndYear.Should().Be(fundingPeriod.EndYear);
		    mappedPeriod.Id.Should().Be(fundingPeriod.Id);
            mappedPeriod.Name.Should().Be(fundingPeriod.Name);
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
            string allocationLineShortName = "short-name";

            Models.Policy.FundingStream fundingStream = new Models.Policy.FundingStream()
            {
                AllocationLines = new List<Models.Policy.AllocationLine>()
                {
                    new Models.Policy.AllocationLine()
                    {
                        Id = allocationLineId,
                        Name = allocationLineName,
                        ShortName = allocationLineShortName,
                        FundingRoute = Models.Policy.FundingRoute.LA,
                        IsContractRequired = true
                    }
                },
                Name = "Name",
                Id = "id",
                ShortName = "short-name",
                PeriodType = new Models.Policy.PeriodType
                {
                    Id = "p1",
                    Name = "period 1",
                    StartDay = 1,
                    EndDay = 31,
                    StartMonth = 8,
                    EndMonth = 7
                }
            };

            // Act
            V1.Models.FundingStream mappedFundingStream = mapperUnderTest.Map<V1.Models.FundingStream>(fundingStream);

            // Assert
            mappedFundingStream.Should().NotBeNull();
            mappedFundingStream.Id.Should().Be(fundingStream.Id);
            mappedFundingStream.Name.Should().Be(fundingStream.Name);
            mappedFundingStream.ShortName.Should().Be(fundingStream.ShortName);
            mappedFundingStream.PeriodType.Id.Should().Be("p1");
            mappedFundingStream.PeriodType.Name.Should().Be("period 1");
            mappedFundingStream.PeriodType.StartDay.Should().Be(1);
            mappedFundingStream.PeriodType.StartMonth.Should().Be(8);
            mappedFundingStream.PeriodType.EndDay.Should().Be(31);
            mappedFundingStream.PeriodType.EndMonth.Should().Be(7);
            mappedFundingStream.AllocationLines.Should().Contain(
                a => a.Id == allocationLineId && 
                a.Name == allocationLineName && a.ShortName == 
                allocationLineShortName && 
                a.FundingRoute == "LA" &&
                a.ContractRequired == "Y")
                .Should().NotBeNull();
        }

    }
}
