using CalculateFunding.Models.Results;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Results.Interfaces;
using FluentAssertions;
using Microsoft.Azure.ServiceBus;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using NSubstitute;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Results.Services
{
    public partial class ResultsServiceTests
    {
        [TestMethod]
        public void FetchProviderProfile_GivenNullMessage_ThrowsArgumentNullException()
        {
            // Arrange
            ResultsService service = CreateResultsService();

            // Act
            Func<Task> action = () => service.FetchProviderProfile(null);

            // Assert
            action.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("message");
        }

        [TestMethod]
        public void FetchProviderProfile_GivenNoPublishedProviderResultId_ThrowsArgumentException()
        {
            // Arrange
            ILogger logger = Substitute.For<ILogger>();
            ResultsService service = CreateResultsService(logger: logger);

            ProviderProfilingRequestModel requestModel = CreateProviderProfilingRequestModel();
            var json = JsonConvert.SerializeObject(requestModel);

            Message message = new Message(Encoding.UTF8.GetBytes(json));

            // Act
            Func<Task> action = () => service.FetchProviderProfile(message);

            // Assert
            action.Should().Throw<ArgumentException>().And.Message.Should().Be("Message must contain a published provider result id");
            logger.Received(1).Error("No Published Provider Result Id was provided to FetchProviderProfile");
        }

        [TestMethod]
        public void FetchProviderProfile_GivenMessageHasNoContent_ThrowsArgumentException()
        {
            // Arrange
            ILogger logger = Substitute.For<ILogger>();
            ResultsService service = CreateResultsService(logger: logger);
            Message message = new Message();
            message.UserProperties["publishedproviderresult-id"] = "test";

            // Act
            Func<Task> action = () => service.FetchProviderProfile(message);

            // Assert
            action.Should().Throw<ArgumentException>().And.Message.Should().Be("Message must contain a provider profiling request");
            logger.Received(1).Error("No Provider Profiling Request was present in the message");
        }

        [TestMethod]
        public void FetchProviderProfile_GivenInvalidPublishedProviderResultId_ThrowsArgumentException()
        {
            // Arrange
            string resultId = "unknown";

            ILogger logger = Substitute.For<ILogger>();
            IPublishedProviderResultsRepository publishedProviderResultsRepository = Substitute.For<IPublishedProviderResultsRepository>();
            publishedProviderResultsRepository
                .GetPublishedProviderResultForId(Arg.Is(resultId))
                .Returns((PublishedProviderResult)null);
            ResultsService service = CreateResultsService(logger: logger, publishedProviderResultsRepository: publishedProviderResultsRepository);

            ProviderProfilingRequestModel requestModel = CreateProviderProfilingRequestModel();
            var json = JsonConvert.SerializeObject(requestModel);

            Message message = new Message(Encoding.UTF8.GetBytes(json));
            message.UserProperties["publishedproviderresult-id"] = resultId;

            // Act
            Func<Task> action = () => service.FetchProviderProfile(message);

            // Assert
            action.Should().Throw<ArgumentException>().And.Message.Should().Be($"Published provider result with id '{resultId}' not found");
            logger.Received(1).Error("Could not find published provider result with id '{id}'", resultId);
        }

        [TestMethod]
        public void FetchProviderProfile_GivenFetchProviderProfileFails_LogsErrorThrowsException()
        {
            // Arrange
            string resultId = "known";
            PublishedProviderResult result = new PublishedProviderResult
            {
                ProviderId = "prov1",
                FundingPeriod = new Models.Specs.Period { EndDate = DateTimeOffset.Now.AddDays(-3), Id = "fp1", Name = "funding 1", StartDate = DateTimeOffset.Now.AddDays(-1) },
                FundingStreamResult = new PublishedFundingStreamResult { AllocationLineResult = new PublishedAllocationLineResult { AllocationLine = new Models.Specs.AllocationLine { Id = "al-1" } } },
                SpecificationId = "spec1"
            };
            ProviderProfilingRequestModel requestModel = CreateProviderProfilingRequestModel();

            ILogger logger = Substitute.For<ILogger>();
            IPublishedProviderResultsRepository publishedProviderResultsRepository = Substitute.For<IPublishedProviderResultsRepository>();
            publishedProviderResultsRepository
                .GetPublishedProviderResultForId(Arg.Is(resultId))
                .Returns(result);
            IProviderProfilingRepository providerProfilingRepository = Substitute.For<IProviderProfilingRepository>();
            providerProfilingRepository
                .GetProviderProfilePeriods(Arg.Is(requestModel))
                .Returns(Task.FromResult<ProviderProfilingResponseModel>(null));
            ResultsService service = CreateResultsService(logger: logger, publishedProviderResultsRepository: publishedProviderResultsRepository, providerProfilingRepository: providerProfilingRepository);

            var json = JsonConvert.SerializeObject(requestModel);

            Message message = new Message(Encoding.UTF8.GetBytes(json));
            message.UserProperties["publishedproviderresult-id"] = resultId;

            // Act
            Func<Task> test = async () => await service.FetchProviderProfile(message);

            // Assert
            test
                .Should()
                .ThrowExactly<Exception>()
                .Which
                .Message
                .Should()
                .Be($"Failed to obtain profiling periods for provider: {result.ProviderId} and period: {result.FundingPeriod.Name}");
        }

        [TestMethod]
        public void FetchProviderProfile_GivenFetchProviderProfileReturnsButWithEmptyDeliveryPeriods_LogsErrorThrowsException()
        {
            // Arrange
            string resultId = "known";
            PublishedProviderResult result = new PublishedProviderResult
            {
                ProviderId = "prov1",
                FundingPeriod = new Models.Specs.Period { EndDate = DateTimeOffset.Now.AddDays(-3), Id = "fp1", Name = "funding 1", StartDate = DateTimeOffset.Now.AddDays(-1) },
                SpecificationId = "spec1",
                FundingStreamResult = new PublishedFundingStreamResult { AllocationLineResult = new PublishedAllocationLineResult { AllocationLine = new Models.Specs.AllocationLine { Id = "al-1" } } }
            };

            ProviderProfilingRequestModel requestModel = CreateProviderProfilingRequestModel();

            ProviderProfilingResponseModel providerProfilingResponseModel = new ProviderProfilingResponseModel();

            ILogger logger = Substitute.For<ILogger>();
            IPublishedProviderResultsRepository publishedProviderResultsRepository = Substitute.For<IPublishedProviderResultsRepository>();
            publishedProviderResultsRepository
                .GetPublishedProviderResultForId(Arg.Is(resultId))
                .Returns(result);
            IProviderProfilingRepository providerProfilingRepository = Substitute.For<IProviderProfilingRepository>();
            providerProfilingRepository
                .GetProviderProfilePeriods(Arg.Is(requestModel))
                .Returns(providerProfilingResponseModel);

            ResultsService service = CreateResultsService(logger: logger, publishedProviderResultsRepository: publishedProviderResultsRepository, providerProfilingRepository: providerProfilingRepository);

            var json = JsonConvert.SerializeObject(requestModel);

            Message message = new Message(Encoding.UTF8.GetBytes(json));
            message.UserProperties["publishedproviderresult-id"] = resultId;

            // Act
            Func<Task> test = async () => await service.FetchProviderProfile(message);

            // Assert
            test
                .Should()
                .ThrowExactly<Exception>()
                .Which
                .Message
                .Should()
                .Be($"Failed to obtain profiling periods for provider: {result.ProviderId} and period: {result.FundingPeriod.Name}");
        }

        [TestMethod]
        public async Task FetchProviderProfile_GivenFetchProviderProfileSucceeds_UpdatesPublishedProviderResult()
        {
            // Arrange
            string specificationId = "spec-id";
            string resultId = "known";
            PublishedProviderResult result = CreatePublishedProviderResults().First();
            result.SpecificationId = specificationId;
            result.FundingStreamResult.AllocationLineResult.Current.Status = AllocationLineStatus.Approved;

            ProviderProfilingRequestModel requestModel = CreateProviderProfilingRequestModel();
            ProviderProfilingResponseModel profileResponse = new ProviderProfilingResponseModel
            {
                DeliveryProfilePeriods = new List<ProfilingPeriod>
                 {
                    new ProfilingPeriod { Period = "Oct", Occurrence = 1, Year = 2018, Type = "CalendarMonth", Value = 82190.0M, DistributionPeriod = "2018-2019" },
                    new ProfilingPeriod { Period = "Apr", Occurrence = 1, Year = 2019, Type = "CalendarMonth", Value = 82190.0M, DistributionPeriod = "2018-2019" }
                 }
            };

            ILogger logger = Substitute.For<ILogger>();
            IPublishedProviderResultsRepository publishedProviderResultsRepository = Substitute.For<IPublishedProviderResultsRepository>();
            publishedProviderResultsRepository
                .GetPublishedProviderResultForId(Arg.Is(resultId))
                .Returns(result);
            IProviderProfilingRepository providerProfilingRepository = Substitute.For<IProviderProfilingRepository>();
            providerProfilingRepository
                .GetProviderProfilePeriods(Arg.Any<ProviderProfilingRequestModel>())
                .Returns(Task.FromResult(profileResponse));

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetCurrentSpecificationById(Arg.Is(specificationId))
                .Returns(CreateSpecification(specificationId));

            ISearchRepository<AllocationNotificationFeedIndex> feedsSearchRepository = CreateAllocationNotificationFeedSearchRepository();

            ResultsService service = CreateResultsService(logger: logger, publishedProviderResultsRepository: publishedProviderResultsRepository, 
                providerProfilingRepository: providerProfilingRepository, specificationsRepository: specificationsRepository, allocationNotificationFeedSearchRepository: feedsSearchRepository);

            var json = JsonConvert.SerializeObject(requestModel);

            Message message = new Message(Encoding.UTF8.GetBytes(json));
            message.UserProperties["publishedproviderresult-id"] = resultId;

            // Act
            await service.FetchProviderProfile(message);

            // Assert
            result.ProfilingPeriods.Should().BeEquivalentTo(profileResponse.DeliveryProfilePeriods, "Profile Periods should be copied onto Published Provider Result");
            IEnumerable<PublishedProviderResult> toBeSavedResults = new List<PublishedProviderResult> { result };
            await publishedProviderResultsRepository.Received(1).SavePublishedResults(Arg.Is<IEnumerable<PublishedProviderResult>>(savedResults => toBeSavedResults.SequenceEqual(savedResults)));
            await feedsSearchRepository.Received(1).Index(Arg.Is<IEnumerable<AllocationNotificationFeedIndex>>(m => m.Count() == 1));

            logger.Received(1).Information(Arg.Is($"Received new provider profiling message for result id {result.Id} and provider {result.ProviderId}"));
            
        }

        private static ProviderProfilingRequestModel CreateProviderProfilingRequestModel()
        {
            return new ProviderProfilingRequestModel
            {
                AllocationValueByDistributionPeriod = new List<AllocationPeriodValue>
                    {
                        new AllocationPeriodValue{ DistributionPeriod = "2018", AllocationValue = 23.3M}
                    },
                FundingStreamPeriod = "2018/2019"
            };
        }
    }
}
