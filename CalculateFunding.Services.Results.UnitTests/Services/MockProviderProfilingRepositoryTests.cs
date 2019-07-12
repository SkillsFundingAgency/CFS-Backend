using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Profiling.Models;
using CalculateFunding.Services.Results.Repositories;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.Results.UnitTests.Services
{
    [TestClass]
    public class MockProviderProfilingRepositoryTests
    {
        [TestMethod]
        public void GetProviderProfilePeriods_GivenNullAllocationValueDistributiuonPeriod_ThrowsArgumentException()
        {
            //Arrange
            ProviderProfilingRequestModel requestModel = new ProviderProfilingRequestModel
            {
                FundingStreamPeriod = "TEST1819"
            };

            MockProviderProfilingRepository repository = new MockProviderProfilingRepository();

            //Act
            Func<Task> test = async () => await repository.GetProviderProfilePeriods(requestModel);

            //Assert
            test
                .Should()
                .ThrowExactly<ArgumentException>();
        }

        [TestMethod]
        public async Task GetProviderProfilePeriods_GivenFundingPeriodWith3Characters_SetsDefaultYear()
        {
            //Arrange
            ProviderProfilingRequestModel requestModel = new ProviderProfilingRequestModel
            {
                AllocationValueByDistributionPeriod = new[]
                 {
                     new Common.ApiClient.Profiling.Models.AllocationPeriodValue
                     {
                         AllocationValue = 1200,
                         DistributionPeriod = "Period"
                     }
                 },
                FundingStreamPeriod = "TES"
            };

            MockProviderProfilingRepository repository = new MockProviderProfilingRepository();

            // Act
            ValidatedApiResponse<ProviderProfilingResponseModel> providerProfilingResponseModel = await repository.GetProviderProfilePeriods(requestModel);

            // Assert
            providerProfilingResponseModel
                .Should()
                .NotBeNull();

            providerProfilingResponseModel
               .Content
               .Should()
               .NotBeNull();

            providerProfilingResponseModel
                .StatusCode
                .Should()
                .Be(HttpStatusCode.OK);

            providerProfilingResponseModel
                .Content
                .DeliveryProfilePeriods
                .ElementAt(0)
                .Year
                .Should()
                .Be(DateTime.Now.Year);

            providerProfilingResponseModel
                .Content
               .DeliveryProfilePeriods
               .ElementAt(1)
               .Year
               .Should()
               .Be(DateTime.Now.Year + 1);
        }

        [TestMethod]
        public async Task GetProviderProfilePeriods_WhenLast4CharcatersAreNotAllIntegers_SetsDefaultYear()
        {
            //Arrange
            ProviderProfilingRequestModel requestModel = new ProviderProfilingRequestModel
            {
                AllocationValueByDistributionPeriod = new[]
                {
                     new Common.ApiClient.Profiling.Models.AllocationPeriodValue
                     {
                         AllocationValue = 1200,
                         DistributionPeriod = "Period"
                     }
                 },
                FundingStreamPeriod = "TEST"
            };

            MockProviderProfilingRepository repository = new MockProviderProfilingRepository();

            // Act
            ValidatedApiResponse<ProviderProfilingResponseModel> providerProfilingResponseModel = await repository.GetProviderProfilePeriods(requestModel);

            // Assert
            providerProfilingResponseModel
                .Should()
                .NotBeNull();

            providerProfilingResponseModel
               .Content
               .Should()
               .NotBeNull();

            providerProfilingResponseModel
                .StatusCode
                .Should()
                .Be(HttpStatusCode.OK);

            providerProfilingResponseModel
                .Content
                .DeliveryProfilePeriods
                .ElementAt(0)
                .Year
                .Should()
                .Be(DateTime.Now.Year);

            providerProfilingResponseModel
               .Content
               .DeliveryProfilePeriods
               .ElementAt(1)
               .Year
               .Should()
               .Be(DateTime.Now.Year + 1);
        }

        [TestMethod]
        public async Task GetProviderProfilePeriods_GivenValidAllocationDistributionPeriod_CalculatesAndReturnsResponseModel()
        {
            //Arrange
            ProviderProfilingRequestModel requestModel = new ProviderProfilingRequestModel
            {
                AllocationValueByDistributionPeriod = new[]
                 {
                     new Common.ApiClient.Profiling.Models.AllocationPeriodValue
                     {
                         AllocationValue = 1200,
                         DistributionPeriod = "Period"
                     }
                 },
                FundingStreamPeriod = "TEST1819"
            };

            MockProviderProfilingRepository repository = new MockProviderProfilingRepository();

            // Act
            ValidatedApiResponse<ProviderProfilingResponseModel> providerProfilingResponseModel = await repository.GetProviderProfilePeriods(requestModel);

            // Assert
            providerProfilingResponseModel
                .Should()
                .NotBeNull();

            providerProfilingResponseModel
               .Content
               .Should()
               .NotBeNull();

            providerProfilingResponseModel
                .StatusCode
                .Should()
                .Be(HttpStatusCode.OK);

            providerProfilingResponseModel.Content.AllocationProfileRequest.Should().BeEquivalentTo(requestModel);
            providerProfilingResponseModel.Content.DeliveryProfilePeriods.ElementAt(0).DistributionPeriod.Should().Be("Period");
            providerProfilingResponseModel.Content.DeliveryProfilePeriods.ElementAt(0).Occurrence.Should().Be(1);
            providerProfilingResponseModel.Content.DeliveryProfilePeriods.ElementAt(0).Period.Should().Be("Oct");
            providerProfilingResponseModel.Content.DeliveryProfilePeriods.ElementAt(0).Type.Should().Be("CalendarMonth");
            providerProfilingResponseModel.Content.DeliveryProfilePeriods.ElementAt(0).Value.Should().Be(700);
            providerProfilingResponseModel.Content.DeliveryProfilePeriods.ElementAt(0).Year.Should().Be(2018);

            providerProfilingResponseModel.Content.AllocationProfileRequest.Should().BeEquivalentTo(requestModel);
            providerProfilingResponseModel.Content.DeliveryProfilePeriods.ElementAt(1).DistributionPeriod.Should().Be("Period");
            providerProfilingResponseModel.Content.DeliveryProfilePeriods.ElementAt(1).Occurrence.Should().Be(1);
            providerProfilingResponseModel.Content.DeliveryProfilePeriods.ElementAt(1).Period.Should().Be("Apr");
            providerProfilingResponseModel.Content.DeliveryProfilePeriods.ElementAt(1).Type.Should().Be("CalendarMonth");
            providerProfilingResponseModel.Content.DeliveryProfilePeriods.ElementAt(1).Value.Should().Be(500);
            providerProfilingResponseModel.Content.DeliveryProfilePeriods.ElementAt(1).Year.Should().Be(2019);
        }
    }
}
