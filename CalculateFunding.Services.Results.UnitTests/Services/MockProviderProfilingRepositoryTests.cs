using CalculateFunding.Models.Results;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Results.Services
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
                     new AllocationPeriodValue
                     {
                         AllocationValue = 1200,
                         DistributionPeriod = "Period"
                     }
                 },
                FundingStreamPeriod = "TES"
            };

            MockProviderProfilingRepository repository = new MockProviderProfilingRepository();

            //Act
            ProviderProfilingResponseModel providerProfilingResponseModel = await repository.GetProviderProfilePeriods(requestModel);

            //Assert
            providerProfilingResponseModel
                .DeliveryProfilePeriods
                .ElementAt(0)
                .Year
                .Should()
                .Be(DateTime.Now.Year);

            providerProfilingResponseModel
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
                     new AllocationPeriodValue
                     {
                         AllocationValue = 1200,
                         DistributionPeriod = "Period"
                     }
                 },
                FundingStreamPeriod = "TEST"
            };

            MockProviderProfilingRepository repository = new MockProviderProfilingRepository();

            //Act
            ProviderProfilingResponseModel providerProfilingResponseModel = await repository.GetProviderProfilePeriods(requestModel);

            //Assert
            providerProfilingResponseModel
                .DeliveryProfilePeriods
                .ElementAt(0)
                .Year
                .Should()
                .Be(DateTime.Now.Year);

            providerProfilingResponseModel
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
                     new AllocationPeriodValue
                     {
                         AllocationValue = 1200,
                         DistributionPeriod = "Period"
                     }
                 },
                FundingStreamPeriod = "TEST1819"
            };

            MockProviderProfilingRepository repository = new MockProviderProfilingRepository();

            //Act
            ProviderProfilingResponseModel providerProfilingResponseModel = await repository.GetProviderProfilePeriods(requestModel);

            //Assert
            providerProfilingResponseModel.AllocationProfileRequest.Should().BeEquivalentTo(requestModel);
            providerProfilingResponseModel.DeliveryProfilePeriods.ElementAt(0).DistributionPeriod.Should().Be("Period");
            providerProfilingResponseModel.DeliveryProfilePeriods.ElementAt(0).Occurrence.Should().Be(1);
            providerProfilingResponseModel.DeliveryProfilePeriods.ElementAt(0).Period.Should().Be("Oct");
            providerProfilingResponseModel.DeliveryProfilePeriods.ElementAt(0).Type.Should().Be("CalendarMonth");
            providerProfilingResponseModel.DeliveryProfilePeriods.ElementAt(0).Value.Should().Be(700);
            providerProfilingResponseModel.DeliveryProfilePeriods.ElementAt(0).Year.Should().Be(2018);

            providerProfilingResponseModel.AllocationProfileRequest.Should().BeEquivalentTo(requestModel);
            providerProfilingResponseModel.DeliveryProfilePeriods.ElementAt(1).DistributionPeriod.Should().Be("Period");
            providerProfilingResponseModel.DeliveryProfilePeriods.ElementAt(1).Occurrence.Should().Be(1);
            providerProfilingResponseModel.DeliveryProfilePeriods.ElementAt(1).Period.Should().Be("Apr");
            providerProfilingResponseModel.DeliveryProfilePeriods.ElementAt(1).Type.Should().Be("CalendarMonth");
            providerProfilingResponseModel.DeliveryProfilePeriods.ElementAt(1).Value.Should().Be(500);
            providerProfilingResponseModel.DeliveryProfilePeriods.ElementAt(1).Year.Should().Be(2019);
        }
    }
}
