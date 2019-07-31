using System;
using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Services.Core.Interfaces.Proxies;
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
            ICalcsApiClientProxy apiClientProxy = Substitute.For<ICalcsApiClientProxy>();
            CalculationsRepository calculationsRepository = new CalculationsRepository(apiClientProxy);
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
            apiClientProxy.DidNotReceive().GetAsync<CalculationSummaryModel>(Arg.Any<string>());
        }

        [TestMethod]
        public void GetCalculationSummariesForSpecification_WhenGivenASpecificationIdInValidFormat_ShouldReturnResult()
        {
            // Arrange 
            List<CalculationSummaryModel> summaryModels = new List<CalculationSummaryModel>()
            {
                new CalculationSummaryModel()
                {
                    Name = "TestCalc",
                    CalculationType = CalculationType.Template,
                    Id = "CalcId"
                }
            };

            ICalcsApiClientProxy mockApi = Substitute.For<ICalcsApiClientProxy>();
            mockApi
                .GetAsync<IEnumerable<CalculationSummaryModel>>(Arg.Any<string>())
                .Returns(summaryModels);

            CalculationsRepository calculationsRepository = new CalculationsRepository(mockApi);
            ArgumentNullException exception = null;

            // Act
            var configuredTaskAwaiter = calculationsRepository.GetCalculationSummariesForSpecification("Test").ConfigureAwait(false).GetAwaiter();
            List<CalculationSummaryModel> calculationSummaryModels = configuredTaskAwaiter.GetResult().ToList();

            // Assert
            calculationSummaryModels.Should().NotBeNull();
            calculationSummaryModels.Should().BeEquivalentTo(summaryModels);
            mockApi.Received(1).GetAsync<IEnumerable<CalculationSummaryModel>>(Arg.Any<string>());
        }

        [TestMethod]
        public void GetBuildProjectBySpecificationId_WhenSpeficationIdIsEmpty_ShouldThrowException()
        {
            // Arrange 
            ICalcsApiClientProxy apiClientProxy = Substitute.For<ICalcsApiClientProxy>();
            CalculationsRepository calculationsRepository = new CalculationsRepository(apiClientProxy);
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
            apiClientProxy.DidNotReceive().GetAsync<BuildProject>(Arg.Any<string>());
        }

        [TestMethod]
        public void GetBuildProjectBySpecificationId_WhenSpeficationIdIsInValidFormat_ShouldReturnResult()
        {
            // Arrange 
            ICalcsApiClientProxy mockApi = Substitute.For<ICalcsApiClientProxy>();
            mockApi
                .GetAsync<BuildProject>(Arg.Any<string>())
                .Returns(new BuildProject());

            CalculationsRepository calculationsRepository = new CalculationsRepository(mockApi);
            ArgumentNullException exception = null;

            // Act
            var configuredTaskAwaiter = calculationsRepository.GetBuildProjectBySpecificationId("Test").ConfigureAwait(false).GetAwaiter();
            BuildProject buildProject = configuredTaskAwaiter.GetResult();

            // Assert
            buildProject.Should().NotBeNull();
            mockApi.Received(1).GetAsync<BuildProject>(Arg.Any<string>());
        }
    }
}
