using AutoMapper;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.Caching;
using CalculateFunding.Models.Scenarios;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.TestEngine.MappingProfiles;
using CalculateFunding.Services.TestRunner.Repositories;
using CalculateFunding.Common.ApiClient.Scenarios;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.TestEngine.UnitTests
{
    [TestClass]
    public class ScenariosRepositoryTests
    {
        [TestMethod]
        public async Task GetTestScenariosBySpecificationId_WhenSpeficationIdIsEmpty_ShouldThrowException()
        {
            // Arrange 
            IScenariosApiClient scenariosApiClient = Substitute.For<IScenariosApiClient>();            
            ICacheProvider cacheProvider = CreateCacheProvider();
            ScenariosRepository scenariosRepository = new ScenariosRepository(scenariosApiClient, cacheProvider,CreateMapper());
            ArgumentNullException exception = null;

            // Act
            try
            {
                IEnumerable<TestScenario> configuredTaskAwaiter = await scenariosRepository.GetTestScenariosBySpecificationId(string.Empty);               
            }
            catch (Exception e)
            {
                exception = e as ArgumentNullException;
            }

            // Assert
            exception.Should().NotBeNull();
            exception.Should().BeOfType<ArgumentNullException>();
            await scenariosApiClient.DidNotReceive().GetTestScenariosBySpecificationId(Arg.Any<string>());
        }

        [TestMethod]
        public async Task GetTestScenariosBySpecificationId_WhenGivenASpecificationIdInValidFormat_ShouldReturnResult()
        {
            // Arrange           

            IEnumerable<CalculateFunding.Common.ApiClient.Scenarios.Models.TestScenario> testScenarios = new List<CalculateFunding.Common.ApiClient.Scenarios.Models.TestScenario>()
            {
                new CalculateFunding.Common.ApiClient.Scenarios.Models.TestScenario()
                {
                    SpecificationId = "Test"                    
                }
            };

            IScenariosApiClient scenariosApiClient = Substitute.For<IScenariosApiClient>();
            scenariosApiClient
                .GetTestScenariosBySpecificationId(Arg.Any<string>())
                .Returns(new ApiResponse<IEnumerable<CalculateFunding.Common.ApiClient.Scenarios.Models.TestScenario>>(HttpStatusCode.OK, testScenarios));

            ScenariosRepository scenariosRepository = new ScenariosRepository(scenariosApiClient, CreateCacheProvider(), CreateMapper());

            // Act
            IEnumerable<TestScenario> result = await scenariosRepository.GetTestScenariosBySpecificationId("Test");

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(testScenarios.Count());
            result.First().SpecificationId.Should().Be(testScenarios.First().SpecificationId);
            await scenariosApiClient.Received(1).GetTestScenariosBySpecificationId(Arg.Any<string>());
        }

        [TestMethod]
        public async Task GetTestScenariosBySpecificationId_WhenGivenApiResponseIsEmpty_ShouldReturnEmptyResult()
        {
            // Arrange 
            IScenariosApiClient scenariosApiClient = Substitute.For<IScenariosApiClient>();
            scenariosApiClient
                .GetTestScenariosBySpecificationId(Arg.Any<string>())
                .Returns(new ApiResponse<IEnumerable<CalculateFunding.Common.ApiClient.Scenarios.Models.TestScenario>>(HttpStatusCode.OK, null));

            ScenariosRepository scenariosRepository = new ScenariosRepository(scenariosApiClient, CreateCacheProvider(), CreateMapper());

            // Act
            IEnumerable<TestScenario> result = await scenariosRepository.GetTestScenariosBySpecificationId("Test");

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(0);
            await scenariosApiClient.Received(1).GetTestScenariosBySpecificationId(Arg.Any<string>());
        }

        [TestMethod]
        public async Task GetTestScenariosBySpecificationId_WhenGivenASpecificationIdInValidFormat_ShouldReturnFail()
        {
            // Arrange           
            string _specificationId = "specificationId";
            IEnumerable<CalculateFunding.Common.ApiClient.Scenarios.Models.TestScenario> testScenarios = new List<CalculateFunding.Common.ApiClient.Scenarios.Models.TestScenario>()
            {
                new CalculateFunding.Common.ApiClient.Scenarios.Models.TestScenario()
                {
                    SpecificationId = _specificationId
                }
            };

            IScenariosApiClient scenariosApiClient = Substitute.For<IScenariosApiClient>();
           
            scenariosApiClient
                .GetTestScenariosBySpecificationId(Arg.Any<string>())
                .Returns(new ApiResponse<IEnumerable<CalculateFunding.Common.ApiClient.Scenarios.Models.TestScenario>>(HttpStatusCode.NotFound, testScenarios));

            ScenariosRepository scenariosRepository = new ScenariosRepository(scenariosApiClient, CreateCacheProvider(), CreateMapper());

            string errorMessage = $"No Test Scenario found for specificationId '{_specificationId}'.";

            // Act

            Func<Task> result = async () => await scenariosRepository.GetTestScenariosBySpecificationId(_specificationId);


            // Assert
            result
                .Should()
                .Throw<RetriableException>()
                .WithMessage(errorMessage);

            await scenariosApiClient.Received(1).GetTestScenariosBySpecificationId(Arg.Any<string>());
        }

        private static ICacheProvider CreateCacheProvider()
        {
            return Substitute.For<ICacheProvider>();
        }

        private static IMapper CreateMapper()
        {
            MapperConfiguration mapperConfig = new MapperConfiguration(c =>
            {
                c.AddProfile<TestEngineMappingProfile>();
            });

            return mapperConfig.CreateMapper();
        }
    }
}
