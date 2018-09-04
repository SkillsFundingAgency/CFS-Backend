using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using AutoMapper.Configuration;
using CalculateFunding.Api.External.MappingProfiles;
using CalculateFunding.Api.External.V1.Models;
using CalculateFunding.Api.External.V1.Services;
using CalculateFunding.Models.Specs;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Specs.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace CalculateFunding.Api.External.UnitTests.Services
{
	[TestClass]
    public class TimePeriodsServiceTests
    {
	    [TestMethod]
	    public async Task GetTimePeriods_WhenServiceReturns200OkResult_ShouldReturnOkResultWithFundingPeriods()
	    {
            // Arrange
            Models.Specs.Period fundingPeriod1 = new Models.Specs.Period()
			{
				Id = "AYCode",
				Name = "AcademicYear",
				StartDate = DateTimeOffset.MinValue,
				EndDate = DateTimeOffset.MaxValue
			};
            Models.Specs.Period fundingPeriod2 = new Models.Specs.Period()
		    {
			    Id = "FYCode",
			    Name = "FinalYear",
			    StartDate = DateTimeOffset.MinValue,
			    EndDate = DateTimeOffset.MaxValue
		    };

			Mapper.Reset();
			MapperConfigurationExpression mappings = new MapperConfigurationExpression();
		    mappings.AddProfile<ExternalApiMappingProfile>();
		    Mapper.Initialize(mappings);
		    IMapper mapper = Mapper.Instance;

            OkObjectResult specServiceOkObjectResult = new OkObjectResult(new List<Models.Specs.Period>
            {
			    fundingPeriod1,
			    fundingPeriod2
		    });

		    ISpecificationsService mockSpecificationsService = Substitute.For<ISpecificationsService>();
			mockSpecificationsService.GetFundingPeriods(Arg.Any<HttpRequest>()).Returns(specServiceOkObjectResult);

		    TimePeriodsService serviceUnderTest = new TimePeriodsService(mockSpecificationsService, mapper);

		    // Act
		    IActionResult result = await serviceUnderTest.GetFundingPeriods(Substitute.For<HttpRequest>());

			// Assert
		    result
			    .Should().NotBeNull()
			    .And
			    .Subject.Should().BeOfType<OkObjectResult>();

		    OkObjectResult resultCasted = result as OkObjectResult;

		    resultCasted.Value
			    .Should().NotBeNull()
			    .And
			    .Subject.Should().BeOfType<List<V1.Models.Period>>();

		    List<V1.Models.Period> resultPeriods = resultCasted.Value as List<V1.Models.Period>;

            resultPeriods
                .Count
                .Should()
                .Be(2);
		}

		[TestMethod]
		public async Task GetTimePeriods_WhenServiceReturns500InternalServerErrorResult_ShouldReturnErrorResult()
		{
			// Arrange
			IMapper mockMapper = Substitute.For<IMapper>();

			ISpecificationsService mockSpecificationsService = Substitute.For<ISpecificationsService>();
			mockSpecificationsService.GetFundingPeriods(Arg.Any<HttpRequest>()).Returns(new InternalServerErrorResult("Doesn't matter message`"));

			TimePeriodsService serviceUnderTest = new TimePeriodsService(mockSpecificationsService, mockMapper);

			// Act
			IActionResult result = await serviceUnderTest.GetFundingPeriods(Substitute.For<HttpRequest>());

			// Assert
			result
				.Should().NotBeNull()
				.And
				.Subject.Should().BeOfType<InternalServerErrorResult>();
		}
	}
}
