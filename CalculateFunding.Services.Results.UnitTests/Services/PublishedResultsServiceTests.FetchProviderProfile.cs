using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CalculateFunding.Common.FeatureToggles;
using CalculateFunding.Models.Results;
using CalculateFunding.Models.Results.Messages;
using CalculateFunding.Models.Results.Search;
using CalculateFunding.Models.Specs;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Results.Interfaces;
using FluentAssertions;
using Microsoft.Azure.ServiceBus;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using NSubstitute;
using Serilog;

namespace CalculateFunding.Services.Results.Services
{
    public partial class PublishedResultsServiceTests
    {
        [TestMethod]
        public void FetchProviderProfile_GivenNullMessage_ThrowsArgumentNullException()
        {
            // Arrange
            PublishedResultsService service = CreateResultsService();

            // Act
            Func<Task> action = () => service.FetchProviderProfile(null);

            // Assert
            action.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("message");
        }

        [TestMethod]
        public void FetchProviderProfile_GivenNoSpecificationId_ThrowsArgumentException()
        {
            // Arrange
            ILogger logger = Substitute.For<ILogger>();
            PublishedResultsService service = CreateResultsService(logger: logger);

            ProviderProfilingRequestModel requestModel = CreateProviderProfilingRequestModel();
            string json = JsonConvert.SerializeObject(requestModel);

            Message message = new Message(Encoding.UTF8.GetBytes(json));

            // Act
            Func<Task> action = () => service.FetchProviderProfile(message);

            // Assert
            action.Should().Throw<ArgumentException>().And.Message.Should().Be("Message must contain a specification id in user properties");
            logger.Received(1).Error("No specification id was present on the message");
        }

        [TestMethod]
        public void FetchProviderProfile_GivenMessageHasNoContent_ThrowsArgumentException()
        {
            // Arrange
            ILogger logger = Substitute.For<ILogger>();
            PublishedResultsService service = CreateResultsService(logger: logger);
            Message message = new Message();
            message.UserProperties["specification-id"] = "test";

            // Act
            Func<Task> action = () => service.FetchProviderProfile(message);

            // Assert
            action.Should().Throw<ArgumentException>().And.Message.Should().Be("Message must contain a collection of allocation results profiling items");
            logger.Received(1).Error("No allocation result profiling items were present in the message");
        }

        [TestMethod]
        public void FetchProviderProfile_GivenSpecificationIdButSpecificationNotFound_ThrowsArgumentException()
        {
            // Arrange
            ILogger logger = Substitute.For<ILogger>();
            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();

            PublishedResultsService service = CreateResultsService(logger: logger, specificationsRepository: specificationsRepository);

            IEnumerable<FetchProviderProfilingMessageItem> requestModel = CreateProfilingMessageItems();
            string json = JsonConvert.SerializeObject(requestModel);

            Message message = new Message(Encoding.UTF8.GetBytes(json));
            message.UserProperties["specification-id"] = "spec1";

            // Act
            Func<Task> action = () => service.FetchProviderProfile(message);

            // Assert
            action.Should().Throw<ArgumentException>().And.Message.Should().Be("Could not find a specification with id spec1");
            logger.Received(1).Error("A specification could not be found with id spec1");
        }

        [TestMethod]
        public void FetchProviderProfile_GivenInvalidPublishedProviderResultId_ThrowsArgumentException()
        {
            // Arrange
            string resultId = "result1";

            ILogger logger = Substitute.For<ILogger>();
            IPublishedProviderResultsRepository publishedProviderResultsRepository = Substitute.For<IPublishedProviderResultsRepository>();
            publishedProviderResultsRepository
                .GetPublishedProviderResultForId(Arg.Is(resultId), Arg.Any<string>())
                .Returns((PublishedProviderResult)null);

            SpecificationCurrentVersion specification = new SpecificationCurrentVersion
            {
                Id = specificationId
            };

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetCurrentSpecificationById(Arg.Is(specificationId))
                .Returns(specification);

            PublishedResultsService service = CreateResultsService(
                logger: logger,
                publishedProviderResultsRepository: publishedProviderResultsRepository,
                specificationsRepository: specificationsRepository);

            IEnumerable<FetchProviderProfilingMessageItem> requestModel = CreateProfilingMessageItems();
            string json = JsonConvert.SerializeObject(requestModel);

            Message message = new Message(Encoding.UTF8.GetBytes(json));
            message.UserProperties["specification-id"] = specificationId;

            // Act
            Func<Task> action = () => service.FetchProviderProfile(message);

            // Assert
            action.Should().Throw<ArgumentException>().And.Message.Should().Be($"Published provider result with id '{resultId}' not found");
        }

        [TestMethod]
        public void FetchProviderProfile_GivenFetchProviderProfileFails_LogsErrorThrowsException()
        {
            // Arrange
            string resultId = "result1";
            PublishedProviderResult result = new PublishedProviderResult
            {
                ProviderId = "prov1",
                FundingPeriod = new Models.Specs.Period { EndDate = DateTimeOffset.Now.AddDays(-3), Id = "fp1", Name = "funding 1", StartDate = DateTimeOffset.Now.AddDays(-1) },
                FundingStreamResult = new PublishedFundingStreamResult
                {
                    AllocationLineResult = new PublishedAllocationLineResult
                    {
                        AllocationLine = new Models.Specs.AllocationLine { Id = "al-1" },
                        Current = new PublishedAllocationLineResultVersion { Value = 100 }
                    },
                    FundingStreamPeriod = "fundingperiod",
                    DistributionPeriod = "dist1"
                },


                SpecificationId = "spec1"
            };
            IEnumerable<FetchProviderProfilingMessageItem> requestModel = CreateProfilingMessageItems();

            ILogger logger = Substitute.For<ILogger>();
            IPublishedProviderResultsRepository publishedProviderResultsRepository = Substitute.For<IPublishedProviderResultsRepository>();
            publishedProviderResultsRepository
                .GetPublishedProviderResultForId(Arg.Is(resultId), Arg.Is(result.ProviderId))
                .Returns(result);
            IProviderProfilingRepository providerProfilingRepository = Substitute.For<IProviderProfilingRepository>();
            providerProfilingRepository
                .GetProviderProfilePeriods(Arg.Any<ProviderProfilingRequestModel>())
                .Returns(Task.FromResult<ProviderProfilingResponseModel>(null));

            SpecificationCurrentVersion specification = new SpecificationCurrentVersion
            {
                Id = specificationId
            };

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetCurrentSpecificationById(Arg.Is(specificationId))
                .Returns(specification);

            PublishedResultsService service = CreateResultsService(
                logger: logger,
                publishedProviderResultsRepository: publishedProviderResultsRepository,
                providerProfilingRepository: providerProfilingRepository,
                specificationsRepository: specificationsRepository);

            string json = JsonConvert.SerializeObject(requestModel);

            Message message = new Message(Encoding.UTF8.GetBytes(json));
            message.UserProperties["specification-id"] = specificationId;

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
            string resultId = "result1";
            PublishedProviderResult result = new PublishedProviderResult
            {
                ProviderId = "prov1",
                FundingPeriod = new Models.Specs.Period { EndDate = DateTimeOffset.Now.AddDays(-3), Id = "fp1", Name = "funding 1", StartDate = DateTimeOffset.Now.AddDays(-1) },
                FundingStreamResult = new PublishedFundingStreamResult
                {
                    AllocationLineResult = new PublishedAllocationLineResult
                    {
                        AllocationLine = new Models.Specs.AllocationLine { Id = "al-1" },
                        Current = new PublishedAllocationLineResultVersion { Value = 100 }
                    },
                    FundingStreamPeriod = "fundingperiod",
                    DistributionPeriod = "dist1"
                },


                SpecificationId = "spec1"
            };

            IEnumerable<FetchProviderProfilingMessageItem> requestModel = CreateProfilingMessageItems();

            ProviderProfilingResponseModel providerProfilingResponseModel = new ProviderProfilingResponseModel();

            ILogger logger = Substitute.For<ILogger>();
            IPublishedProviderResultsRepository publishedProviderResultsRepository = Substitute.For<IPublishedProviderResultsRepository>();
            publishedProviderResultsRepository
                .GetPublishedProviderResultForId(Arg.Is(resultId), Arg.Is(result.ProviderId))
                .Returns(result);
            IProviderProfilingRepository providerProfilingRepository = Substitute.For<IProviderProfilingRepository>();
            providerProfilingRepository
                .GetProviderProfilePeriods(Arg.Any<ProviderProfilingRequestModel>())
                .Returns(providerProfilingResponseModel);

            SpecificationCurrentVersion specification = new SpecificationCurrentVersion
            {
                Id = specificationId
            };

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetCurrentSpecificationById(Arg.Is(specificationId))
                .Returns(specification);

            PublishedResultsService service = CreateResultsService(
                logger: logger,
                publishedProviderResultsRepository: publishedProviderResultsRepository,
                providerProfilingRepository: providerProfilingRepository,
                specificationsRepository: specificationsRepository);

            string json = JsonConvert.SerializeObject(requestModel);

            Message message = new Message(Encoding.UTF8.GetBytes(json));
            message.UserProperties["specification-id"] = specificationId;

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
        public async Task FetchProviderProfile_GivenFetchProviderProfileSucceedsAndMajorMinorIsEnabled_UpdatesPublishedProviderResult()
        {
            // Arrange
            PublishedProviderResult result = CreatePublishedProviderResults().First();
            result.SpecificationId = specificationId;
            result.FundingStreamResult.AllocationLineResult.Current.Status = AllocationLineStatus.Approved;
            result.FundingStreamResult.AllocationLineResult.Current.Major = 1;
            result.FundingStreamResult.AllocationLineResult.Current.Minor = 1;

            IEnumerable<FetchProviderProfilingMessageItem> requestModel = CreateProfilingMessageItems();
            requestModel.First().ProviderId = result.ProviderId;
            requestModel.First().AllocationLineResultId = result.Id;

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
                .GetPublishedProviderResultForId(Arg.Is(result.Id), Arg.Is(result.ProviderId))
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

            SpecificationCurrentVersion specification = new SpecificationCurrentVersion
            {
                Id = specificationId
            };

            IFeatureToggle featureToggler = Substitute.For<IFeatureToggle>();
            featureToggler
                .IsAllocationLineMajorMinorVersioningEnabled()
                .Returns(true);

            PublishedResultsService service = CreateResultsService(logger: logger, publishedProviderResultsRepository: publishedProviderResultsRepository,
                providerProfilingRepository: providerProfilingRepository, specificationsRepository: specificationsRepository, allocationNotificationFeedSearchRepository: feedsSearchRepository, featureToggle: featureToggler);

            string json = JsonConvert.SerializeObject(requestModel);

            Message message = new Message(Encoding.UTF8.GetBytes(json));
            message.UserProperties["specification-id"] = specificationId;

            // Act
            await service.FetchProviderProfile(message);

            // Assert
            result.ProfilingPeriods.Should().BeEquivalentTo(profileResponse.DeliveryProfilePeriods, "Profile Periods should be copied onto Published Provider Result");
            IEnumerable<PublishedProviderResult> toBeSavedResults = new List<PublishedProviderResult> { result };
            await publishedProviderResultsRepository.Received(1).SavePublishedResults(Arg.Is<IEnumerable<PublishedProviderResult>>(savedResults => toBeSavedResults.SequenceEqual(savedResults)));
            await feedsSearchRepository.Received(1).Index(Arg.Is<IEnumerable<AllocationNotificationFeedIndex>>(m => m.Count() == 1 && m.First().MajorVersion == 1 && m.First().MinorVersion == 1));
            await providerProfilingRepository.Received(1).GetProviderProfilePeriods(Arg.Is<ProviderProfilingRequestModel>(m =>
                m.AllocationValueByDistributionPeriod.First().AllocationValue == 50
            ));
        }

        [TestMethod]
        public async Task FetchProviderProfile_GivenFetchProviderProfileSucceedsAndMajorMinorIsDisabled_UpdatesPublishedProviderResultDoesNotIndexMajorMinor()
        {
            // Arrange
            PublishedProviderResult result = CreatePublishedProviderResults().First();
            result.SpecificationId = specificationId;
            result.FundingStreamResult.AllocationLineResult.Current.Status = AllocationLineStatus.Approved;
            result.FundingStreamResult.AllocationLineResult.Current.Major = 1;
            result.FundingStreamResult.AllocationLineResult.Current.Minor = 1;

            IEnumerable<FetchProviderProfilingMessageItem> requestModel = CreateProfilingMessageItems();
            requestModel.First().ProviderId = result.ProviderId;
            requestModel.First().AllocationLineResultId = result.Id;

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
                .GetPublishedProviderResultForId(Arg.Is(result.Id), Arg.Is(result.ProviderId))
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

            SpecificationCurrentVersion specification = new SpecificationCurrentVersion
            {
                Id = specificationId
            };

            IFeatureToggle featureToggle = Substitute.For<IFeatureToggle>();
            featureToggle
                .IsAllocationLineMajorMinorVersioningEnabled()
                .Returns(false);

            PublishedResultsService service = CreateResultsService(logger: logger, publishedProviderResultsRepository: publishedProviderResultsRepository,
                providerProfilingRepository: providerProfilingRepository, specificationsRepository: specificationsRepository, allocationNotificationFeedSearchRepository: feedsSearchRepository, featureToggle: featureToggle);

            string json = JsonConvert.SerializeObject(requestModel);

            Message message = new Message(Encoding.UTF8.GetBytes(json));
            message.UserProperties["specification-id"] = specificationId;

            // Act
            await service.FetchProviderProfile(message);

            // Assert
            result.ProfilingPeriods.Should().BeEquivalentTo(profileResponse.DeliveryProfilePeriods, "Profile Periods should be copied onto Published Provider Result");
            IEnumerable<PublishedProviderResult> toBeSavedResults = new List<PublishedProviderResult> { result };
            await publishedProviderResultsRepository.Received(1).SavePublishedResults(Arg.Is<IEnumerable<PublishedProviderResult>>(savedResults => toBeSavedResults.SequenceEqual(savedResults)));
            await feedsSearchRepository.Received(1).Index(Arg.Is<IEnumerable<AllocationNotificationFeedIndex>>(m => m.Count() == 1 && m.First().MajorVersion == null && m.First().MinorVersion == null));
            await providerProfilingRepository.Received(1).GetProviderProfilePeriods(Arg.Is<ProviderProfilingRequestModel>(m =>
                m.AllocationValueByDistributionPeriod.First().AllocationValue == 50
            ));
        }

        [TestMethod]
        public async Task FetchProviderProfile_GivenFetchProviderWithBatchOf3ProfileSucceeds_UpdatesPublishedProviderResult()
        {
            // Arrange
            IEnumerable<PublishedProviderResult> results = CreatePublishedProviderResultsWithDifferentProviders();

            foreach (PublishedProviderResult result in results)
            {
                result.SpecificationId = specificationId;
                result.FundingStreamResult.AllocationLineResult.Current.Status = AllocationLineStatus.Approved;
            }

            ProviderProfilingResponseModel profileResponse1 = new ProviderProfilingResponseModel
            {
                DeliveryProfilePeriods = new List<ProfilingPeriod>
                 {
                    new ProfilingPeriod { Period = "Oct", Occurrence = 1, Year = 2018, Type = "CalendarMonth", Value = 82190.0M, DistributionPeriod = "2018-2019" },
                    new ProfilingPeriod { Period = "Apr", Occurrence = 1, Year = 2019, Type = "CalendarMonth", Value = 82190.0M, DistributionPeriod = "2018-2019" }
                 }
            };

            ProviderProfilingResponseModel profileResponse2 = new ProviderProfilingResponseModel
            {
                DeliveryProfilePeriods = new List<ProfilingPeriod>
                 {
                    new ProfilingPeriod { Period = "Oct", Occurrence = 1, Year = 2018, Type = "CalendarMonth", Value = 52190.0M, DistributionPeriod = "2018-2019" },
                    new ProfilingPeriod { Period = "Apr", Occurrence = 1, Year = 2019, Type = "CalendarMonth", Value = 52190.0M, DistributionPeriod = "2018-2019" }
                 }
            };

            ProviderProfilingResponseModel profileResponse3 = new ProviderProfilingResponseModel
            {
                DeliveryProfilePeriods = new List<ProfilingPeriod>
                 {
                    new ProfilingPeriod { Period = "Oct", Occurrence = 1, Year = 2018, Type = "CalendarMonth", Value = 32190.0M, DistributionPeriod = "2018-2019" },
                    new ProfilingPeriod { Period = "Apr", Occurrence = 1, Year = 2019, Type = "CalendarMonth", Value = 32190.0M, DistributionPeriod = "2018-2019" }
                 }
            };

            ILogger logger = Substitute.For<ILogger>();
            IPublishedProviderResultsRepository publishedProviderResultsRepository = Substitute.For<IPublishedProviderResultsRepository>();
            publishedProviderResultsRepository
                .GetPublishedProviderResultForId(Arg.Any<string>(), Arg.Any<string>())
                .Returns(results.ElementAt(0), results.ElementAt(1), results.ElementAt(2));

            IProviderProfilingRepository providerProfilingRepository = Substitute.For<IProviderProfilingRepository>();
            providerProfilingRepository
                .GetProviderProfilePeriods(Arg.Any<ProviderProfilingRequestModel>())
                .Returns(profileResponse1, profileResponse2, profileResponse3);

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetCurrentSpecificationById(Arg.Is(specificationId))
                .Returns(CreateSpecification(specificationId));

            ISearchRepository<AllocationNotificationFeedIndex> feedsSearchRepository = CreateAllocationNotificationFeedSearchRepository();

            SpecificationCurrentVersion specification = new SpecificationCurrentVersion
            {
                Id = specificationId
            };

            IEnumerable<FetchProviderProfilingMessageItem> requestModel = new[]
            {
                new FetchProviderProfilingMessageItem { ProviderId = results.ElementAt(0).ProviderId, AllocationLineResultId = results.ElementAt(0).Id },
                new FetchProviderProfilingMessageItem { ProviderId = results.ElementAt(1).ProviderId, AllocationLineResultId = results.ElementAt(1).Id },
                new FetchProviderProfilingMessageItem { ProviderId = results.ElementAt(2).ProviderId, AllocationLineResultId = results.ElementAt(2).Id }
            };

            PublishedResultsService service = CreateResultsService(logger: logger, publishedProviderResultsRepository: publishedProviderResultsRepository,
                providerProfilingRepository: providerProfilingRepository, specificationsRepository: specificationsRepository, allocationNotificationFeedSearchRepository: feedsSearchRepository);

            string json = JsonConvert.SerializeObject(requestModel);

            Message message = new Message(Encoding.UTF8.GetBytes(json));
            message.UserProperties["specification-id"] = specificationId;

            // Act
            await service.FetchProviderProfile(message);

            // Assert
            results.ElementAt(0).ProfilingPeriods.Should().NotBeNullOrEmpty();
            results.ElementAt(1).ProfilingPeriods.Should().NotBeNullOrEmpty();
            results.ElementAt(2).ProfilingPeriods.Should().NotBeNullOrEmpty();
            IEnumerable<PublishedProviderResult> toBeSavedResults = new List<PublishedProviderResult> { results.ElementAt(0), results.ElementAt(1), results.ElementAt(2) };

            await publishedProviderResultsRepository.Received(1).SavePublishedResults(Arg.Is<IEnumerable<PublishedProviderResult>>(m => m.Count() == 3));
            await feedsSearchRepository.Received(1).Index(Arg.Is<IEnumerable<AllocationNotificationFeedIndex>>(m => m.Count() == 3));
            await providerProfilingRepository.Received(1).GetProviderProfilePeriods(Arg.Is<ProviderProfilingRequestModel>(m =>
                m.AllocationValueByDistributionPeriod.First().AllocationValue == 50
            ));
            await providerProfilingRepository.Received(2).GetProviderProfilePeriods(Arg.Is<ProviderProfilingRequestModel>(m =>
                m.AllocationValueByDistributionPeriod.First().AllocationValue == 100
            ));
        }

        [TestMethod]
        public async Task FetchProviderProfile_GivenFetchProviderWithBatchOf3ButOneFailsToProfile_DoesnotUpdateResults()
        {
            // Arrange
            IEnumerable<PublishedProviderResult> results = CreatePublishedProviderResultsWithDifferentProviders();

            foreach (PublishedProviderResult result in results)
            {
                result.SpecificationId = specificationId;
                result.FundingStreamResult.AllocationLineResult.Current.Status = AllocationLineStatus.Approved;
            }

            ProviderProfilingResponseModel profileResponse1 = new ProviderProfilingResponseModel
            {
                DeliveryProfilePeriods = new List<ProfilingPeriod>
                 {
                    new ProfilingPeriod { Period = "Oct", Occurrence = 1, Year = 2018, Type = "CalendarMonth", Value = 82190.0M, DistributionPeriod = "2018-2019" },
                    new ProfilingPeriod { Period = "Apr", Occurrence = 1, Year = 2019, Type = "CalendarMonth", Value = 82190.0M, DistributionPeriod = "2018-2019" }
                 }
            };

            ProviderProfilingResponseModel profileResponse2 = new ProviderProfilingResponseModel
            {
                DeliveryProfilePeriods = new List<ProfilingPeriod>
                 {
                    new ProfilingPeriod { Period = "Oct", Occurrence = 1, Year = 2018, Type = "CalendarMonth", Value = 52190.0M, DistributionPeriod = "2018-2019" },
                    new ProfilingPeriod { Period = "Apr", Occurrence = 1, Year = 2019, Type = "CalendarMonth", Value = 52190.0M, DistributionPeriod = "2018-2019" }
                 }
            };

            ILogger logger = Substitute.For<ILogger>();
            IPublishedProviderResultsRepository publishedProviderResultsRepository = Substitute.For<IPublishedProviderResultsRepository>();
            publishedProviderResultsRepository
                .GetPublishedProviderResultForId(Arg.Any<string>(), Arg.Any<string>())
                .Returns(results.ElementAt(0), results.ElementAt(1), results.ElementAt(2));

            IProviderProfilingRepository providerProfilingRepository = Substitute.For<IProviderProfilingRepository>();
            providerProfilingRepository
                .GetProviderProfilePeriods(Arg.Any<ProviderProfilingRequestModel>())
                .Returns(profileResponse1, profileResponse2, null);

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetCurrentSpecificationById(Arg.Is(specificationId))
                .Returns(CreateSpecification(specificationId));

            ISearchRepository<AllocationNotificationFeedIndex> feedsSearchRepository = CreateAllocationNotificationFeedSearchRepository();

            SpecificationCurrentVersion specification = new SpecificationCurrentVersion
            {
                Id = specificationId
            };

            IEnumerable<FetchProviderProfilingMessageItem> requestModel = new[]
            {
                new FetchProviderProfilingMessageItem { ProviderId = results.ElementAt(0).ProviderId, AllocationLineResultId = results.ElementAt(0).Id },
                new FetchProviderProfilingMessageItem { ProviderId = results.ElementAt(1).ProviderId, AllocationLineResultId = results.ElementAt(1).Id },
                new FetchProviderProfilingMessageItem { ProviderId = results.ElementAt(2).ProviderId, AllocationLineResultId = results.ElementAt(2).Id }
            };

            PublishedResultsService service = CreateResultsService(logger: logger, publishedProviderResultsRepository: publishedProviderResultsRepository,
                providerProfilingRepository: providerProfilingRepository, specificationsRepository: specificationsRepository, allocationNotificationFeedSearchRepository: feedsSearchRepository);

            string json = JsonConvert.SerializeObject(requestModel);

            Message message = new Message(Encoding.UTF8.GetBytes(json));
            message.UserProperties["specification-id"] = specificationId;

            // Act
            Func<Task> test = () => service.FetchProviderProfile(message);

            // Assert
            test
               .Should()
               .ThrowExactly<Exception>()
               .Which
               .Message
               .Should()
               .NotBeNullOrWhiteSpace();

            await publishedProviderResultsRepository.DidNotReceive().SavePublishedResults(Arg.Any<IEnumerable<PublishedProviderResult>>());
            await feedsSearchRepository.DidNotReceive().Index(Arg.Any<IEnumerable<AllocationNotificationFeedIndex>>());
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

        private static IEnumerable<FetchProviderProfilingMessageItem> CreateProfilingMessageItems()
        {
            return new[]
            {
                new FetchProviderProfilingMessageItem
                {
                    ProviderId = "prov1",
                    AllocationLineResultId = "result1"
                }
            };
        }
    }
}
