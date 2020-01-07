using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using AutoMapper;
using CalculateFunding.Common.ApiClient.Calcs;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Services.Calcs.MappingProfiles;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace CalculateFunding.Services.CalcEngine.UnitTests
{
    [TestClass]
    public class CalculationRepositoryTests
    {
        [TestMethod]
        public void GetCalculationSummariesForSpecification_WhenSpeficationIdIsEmpty_ShouldThrowException()
        {
            // Arrange 
            ICalculationsApiClient apiClientProxy = Substitute.For<ICalculationsApiClient>();
            IMapper mapper = CreateMapper();
            CalculationsRepository calculationsRepository = new CalculationsRepository(apiClientProxy, CreateMapper());
            ArgumentNullException exception = null;

            // Act
            try
            {
                var configuredTaskAwaiter = calculationsRepository.GetCalculationSummariesForSpecification(string.Empty).ConfigureAwait(false).GetAwaiter();
                configuredTaskAwaiter.GetResult();
            }
            catch (Exception e)
            {
                exception = e as ArgumentNullException;
            }

            // Assert
            exception.Should().NotBeNull();
            exception.Should().BeOfType<ArgumentNullException>();
            apiClientProxy.DidNotReceive().GetCalculationSummariesForSpecification(Arg.Any<string>());
        }

        [TestMethod]
        public void GetCalculationSummariesForSpecification_WhenGivenASpecificationIdInValidFormat_ShouldReturnResult()
        {
            // Arrange 
            List<Common.ApiClient.Calcs.Models.CalculationSummary> summaryModels = new List<Common.ApiClient.Calcs.Models.CalculationSummary>()
            {
                new Common.ApiClient.Calcs.Models.CalculationSummary()
                {
                    Name = "TestCalc",
                    CalculationType = Common.ApiClient.Calcs.Models.CalculationType.Template,
                    Id = "CalcId"
                }
            };

            ICalculationsApiClient mockApi = Substitute.For<ICalculationsApiClient>();
            mockApi
                .GetCalculationSummariesForSpecification(Arg.Any<string>())
                .Returns(new ApiResponse<IEnumerable<Common.ApiClient.Calcs.Models.CalculationSummary>>(HttpStatusCode.OK, summaryModels));

            CalculationsRepository calculationsRepository = new CalculationsRepository(mockApi, CreateMapper());
            ArgumentNullException exception = null;

            // Act
            var configuredTaskAwaiter = calculationsRepository.GetCalculationSummariesForSpecification("Test").ConfigureAwait(false).GetAwaiter();
            List<CalculationSummaryModel> calculationSummaryModels = configuredTaskAwaiter.GetResult().ToList();

            // Assert
            calculationSummaryModels.Should().NotBeNull();
            calculationSummaryModels.Should().BeEquivalentTo(summaryModels);
            mockApi.Received(1).GetCalculationSummariesForSpecification(Arg.Any<string>());
        }

        [TestMethod]
        public void GetBuildProjectBySpecificationId_WhenSpeficationIdIsEmpty_ShouldThrowException()
        {
            // Arrange 
            ICalculationsApiClient apiClientProxy = Substitute.For<ICalculationsApiClient>();
            CalculationsRepository calculationsRepository = new CalculationsRepository(apiClientProxy, CreateMapper());
            ArgumentNullException exception = null;

            // Act
            try
            {
                var configuredTaskAwaiter = calculationsRepository.GetBuildProjectBySpecificationId(string.Empty).ConfigureAwait(false).GetAwaiter();
                configuredTaskAwaiter.GetResult();
            }
            catch (Exception e)
            {
                exception = e as ArgumentNullException;
            }

            // Assert
            exception.Should().NotBeNull();
            exception.Should().BeOfType<ArgumentNullException>();
            apiClientProxy.DidNotReceive().GetBuildProjectBySpecificationId(Arg.Any<string>());
        }

        [TestMethod]
        public void GetBuildProjectBySpecificationId_WhenSpeficationIdIsInValidFormat_ShouldReturnResult()
        {
            // Arrange 
            ICalculationsApiClient mockApi = Substitute.For<ICalculationsApiClient>();
            mockApi
                .GetBuildProjectBySpecificationId(Arg.Any<string>())
                .Returns(new ApiResponse<Common.ApiClient.Calcs.Models.BuildProject>(HttpStatusCode.OK, new Common.ApiClient.Calcs.Models.BuildProject()));

            CalculationsRepository calculationsRepository = new CalculationsRepository(mockApi, CreateMapper());
            ArgumentNullException exception = null;

            // Act
            var configuredTaskAwaiter = calculationsRepository.GetBuildProjectBySpecificationId("Test").ConfigureAwait(false).GetAwaiter();
            BuildProject buildProject = configuredTaskAwaiter.GetResult();

            // Assert
            buildProject.Should().NotBeNull();
            mockApi.Received(1).GetBuildProjectBySpecificationId(Arg.Any<string>());
        }

        private static IMapper CreateMapper()
        {
            MapperConfiguration mapperConfig = new MapperConfiguration(c =>
            {
                c.AddProfile<CalculationsMappingProfile>();
            });

            return mapperConfig.CreateMapper();
        }
    }
}
