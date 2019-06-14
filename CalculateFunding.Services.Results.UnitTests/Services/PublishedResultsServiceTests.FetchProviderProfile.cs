using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Jobs;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Profiling;
using CalculateFunding.Common.ApiClient.Profiling.Models;
using CalculateFunding.Common.FeatureToggles;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Results;
using CalculateFunding.Models.Results.Messages;
using CalculateFunding.Models.Results.Search;
using CalculateFunding.Models.Specs;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Core;
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
        public async Task FetchProviderProfile_GivenNoSpecificationId_LogsAndStopsProcessing()
        {
            // Arrange
            ILogger logger = Substitute.For<ILogger>();
            IJobsApiClient jobsApiClient = CreateJobsApiClient();
            jobsApiClient
                .GetJobById(Arg.Is(jobId))
                .Returns(new ApiResponse<JobViewModel>(HttpStatusCode.OK, new JobViewModel { Id = jobId }));

            PublishedResultsService service = CreateResultsService(logger: logger, jobsApiClient: jobsApiClient);

            ProviderProfilingRequestModel requestModel = CreateProviderProfilingRequestModel();
            string json = JsonConvert.SerializeObject(requestModel);

            Message message = new Message(Encoding.UTF8.GetBytes(json));
            message.UserProperties.Add("jobId", jobId);

            // Act
            await service.FetchProviderProfile(message);

            // Assert
            logger.Received(1).Error("No specification id was present on the message");
        }

        [TestMethod]
        public async Task FetchProviderProfile_GivenMessageHasNoContent_LogsAndStopsProcessing()
        {
            // Arrange
            ILogger logger = Substitute.For<ILogger>();
            IJobsApiClient jobsApiClient = CreateJobsApiClient();
            jobsApiClient
                .GetJobById(Arg.Is(jobId))
                .Returns(new ApiResponse<JobViewModel>(HttpStatusCode.OK, new JobViewModel { Id = jobId }));

            PublishedResultsService service = CreateResultsService(logger: logger, jobsApiClient: jobsApiClient);

            Message message = new Message();
            message.UserProperties["specification-id"] = "test";
            message.UserProperties["jobId"] = jobId;

            // Act
            await service.FetchProviderProfile(message);

            // Assert
            logger.Received(1).Error("No allocation result profiling items were present in the message");
        }

        [TestMethod]
        public async Task FetchProviderProfile_GivenSpecificationIdButSpecificationNotFound_LogsAndStopsProcessing()
        {
            // Arrange
            ILogger logger = Substitute.For<ILogger>();
            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();

            IJobsApiClient jobsApiClient = CreateJobsApiClient();
            jobsApiClient
                .GetJobById(Arg.Is(jobId))
                .Returns(new ApiResponse<JobViewModel>(HttpStatusCode.OK, new JobViewModel { Id = jobId }));

            PublishedResultsService service = CreateResultsService(logger: logger, specificationsRepository: specificationsRepository, jobsApiClient: jobsApiClient);

            IEnumerable<FetchProviderProfilingMessageItem> requestModel = CreateProfilingMessageItems();
            string json = JsonConvert.SerializeObject(requestModel);

            Message message = new Message(Encoding.UTF8.GetBytes(json));
            message.UserProperties["specification-id"] = "spec1";
            message.UserProperties["jobId"] = jobId;

            // Act
            await service.FetchProviderProfile(message);

            // Assert
            logger.Received(1).Error("A specification could not be found with id spec1");
        }

        [TestMethod]
        public async Task FetchProviderProfile_GivenInvalidPublishedProviderResultId_LogsAndStopsProcessing()
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

            IJobsApiClient jobsApiClient = CreateJobsApiClient();
            jobsApiClient
                .GetJobById(Arg.Is(jobId))
                .Returns(new ApiResponse<JobViewModel>(HttpStatusCode.OK, new JobViewModel { Id = jobId }));

            PublishedResultsService service = CreateResultsService(
                logger: logger,
                publishedProviderResultsRepository: publishedProviderResultsRepository,
                specificationsRepository: specificationsRepository,
                jobsApiClient: jobsApiClient);

            IEnumerable<FetchProviderProfilingMessageItem> requestModel = CreateProfilingMessageItems();
            string json = JsonConvert.SerializeObject(requestModel);

            Message message = new Message(Encoding.UTF8.GetBytes(json));
            message.UserProperties["specification-id"] = specificationId;
            message.UserProperties["jobId"] = jobId;

            // Act
            await service.FetchProviderProfile(message);

            // Assert
            logger
                .Received(1)
                .Error(Arg.Is("Could not find published provider result with id '{id}'"), Arg.Any<string>());
        }

        [TestMethod]
        public void FetchProviderProfile_GivenFetchProviderProfileFails_LogsErrorThrowsRetriableException()
        {
            // Arrange
            string resultId = "result1";
            PublishedProviderResult result = new PublishedProviderResult
            {
                ProviderId = "prov1",
                FundingPeriod = new Models.Specs.Period { EndDate = DateTimeOffset.Now.AddDays(-3), Id = "fp18", Name = "funding 1", StartDate = DateTimeOffset.Now.AddDays(-1) },
                FundingStreamResult = new PublishedFundingStreamResult
                {
                    AllocationLineResult = new PublishedAllocationLineResult
                    {
                        AllocationLine = new PublishedAllocationLineDefinition { Id = "al-1" },
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
            IProfilingApiClient providerProfilingRepository = Substitute.For<IProfilingApiClient>();
            providerProfilingRepository
                .GetProviderProfilePeriods(Arg.Any<ProviderProfilingRequestModel>())
                .Returns(Task.FromResult<ValidatedApiResponse<ProviderProfilingResponseModel>>(null));

            SpecificationCurrentVersion specification = new SpecificationCurrentVersion
            {
                Id = specificationId
            };

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetCurrentSpecificationById(Arg.Is(specificationId))
                .Returns(specification);

            IJobsApiClient jobsApiClient = CreateJobsApiClient();
            jobsApiClient
                .GetJobById(Arg.Is(jobId))
                .Returns(new ApiResponse<JobViewModel>(HttpStatusCode.OK, new JobViewModel { Id = jobId }));

            PublishedResultsService service = CreateResultsService(
                logger: logger,
                publishedProviderResultsRepository: publishedProviderResultsRepository,
                profilingApiClient: providerProfilingRepository,
                specificationsRepository: specificationsRepository,
                jobsApiClient: jobsApiClient);

            string json = JsonConvert.SerializeObject(requestModel);

            Message message = new Message(Encoding.UTF8.GetBytes(json));
            message.UserProperties["specification-id"] = specificationId;
            message.UserProperties["jobId"] = jobId;

            // Act
            Func<Task> test = async () => await service.FetchProviderProfile(message);

            // Assert
            test
                .Should()
                .ThrowExactly<RetriableException>()
                .Which
                .Message
                .Should()
                .Be($"Failed to obtain profiling periods for provider: {result.ProviderId} and period: {result.FundingPeriod.Name}. Status Code = ''");
        }

        [TestMethod]
        public void FetchProviderProfile_GivenFetchProviderProfileFailsWithSpecificHttpReturnCode_LogsErrorThrowsRetriableException()
        {
            // Arrange
            string resultId = "result1";
            PublishedProviderResult result = new PublishedProviderResult
            {
                ProviderId = "prov1",
                FundingPeriod = new Models.Specs.Period { EndDate = DateTimeOffset.Now.AddDays(-3), Id = "fp18", Name = "funding 1", StartDate = DateTimeOffset.Now.AddDays(-1) },
                FundingStreamResult = new PublishedFundingStreamResult
                {
                    AllocationLineResult = new PublishedAllocationLineResult
                    {
                        AllocationLine = new PublishedAllocationLineDefinition { Id = "al-1" },
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
            IProfilingApiClient providerProfilingRepository = Substitute.For<IProfilingApiClient>();
            providerProfilingRepository
                .GetProviderProfilePeriods(Arg.Any<ProviderProfilingRequestModel>())
                .Returns(Task.FromResult<ValidatedApiResponse<ProviderProfilingResponseModel>>(new ValidatedApiResponse<ProviderProfilingResponseModel>(HttpStatusCode.InternalServerError, null)));

            SpecificationCurrentVersion specification = new SpecificationCurrentVersion
            {
                Id = specificationId
            };

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetCurrentSpecificationById(Arg.Is(specificationId))
                .Returns(specification);

            IJobsApiClient jobsApiClient = CreateJobsApiClient();
            jobsApiClient
                .GetJobById(Arg.Is(jobId))
                .Returns(new ApiResponse<JobViewModel>(HttpStatusCode.OK, new JobViewModel { Id = jobId }));

            PublishedResultsService service = CreateResultsService(
                logger: logger,
                publishedProviderResultsRepository: publishedProviderResultsRepository,
                profilingApiClient: providerProfilingRepository,
                specificationsRepository: specificationsRepository,
                jobsApiClient: jobsApiClient);

            string json = JsonConvert.SerializeObject(requestModel);

            Message message = new Message(Encoding.UTF8.GetBytes(json));
            message.UserProperties["specification-id"] = specificationId;
            message.UserProperties["jobId"] = jobId;

            // Act
            Func<Task> test = async () => await service.FetchProviderProfile(message);

            // Assert
            test
                .Should()
                .ThrowExactly<RetriableException>()
                .Which
                .Message
                .Should()
                .Be($"Failed to obtain profiling periods for provider: {result.ProviderId} and period: {result.FundingPeriod.Name}. Status Code = 'InternalServerError'");
        }

        [TestMethod]
        public void FetchProviderProfile_GivenFetchProviderProfileReturnsButWithEmptyDeliveryPeriods_LogsErrorThrowsRetriableException()
        {
            // Arrange
            string resultId = "result1";
            PublishedProviderResult result = new PublishedProviderResult
            {
                ProviderId = "prov1",
                FundingPeriod = new Models.Specs.Period { EndDate = DateTimeOffset.Now.AddDays(-3), Id = "fp18", Name = "funding 1", StartDate = DateTimeOffset.Now.AddDays(-1) },
                FundingStreamResult = new PublishedFundingStreamResult
                {
                    AllocationLineResult = new PublishedAllocationLineResult
                    {
                        AllocationLine = new PublishedAllocationLineDefinition { Id = "al-1" },
                        Current = new PublishedAllocationLineResultVersion { Value = 100 }
                    },
                    FundingStreamPeriod = "fundingperiod",
                    DistributionPeriod = "dist1"
                },


                SpecificationId = "spec1"
            };

            IEnumerable<FetchProviderProfilingMessageItem> requestModel = CreateProfilingMessageItems();

            ValidatedApiResponse<ProviderProfilingResponseModel> providerProfilingResponseModel = new ValidatedApiResponse<ProviderProfilingResponseModel>(HttpStatusCode.InternalServerError);

            ILogger logger = Substitute.For<ILogger>();
            IPublishedProviderResultsRepository publishedProviderResultsRepository = Substitute.For<IPublishedProviderResultsRepository>();
            publishedProviderResultsRepository
                .GetPublishedProviderResultForId(Arg.Is(resultId), Arg.Is(result.ProviderId))
                .Returns(result);
            IProfilingApiClient providerProfilingRepository = Substitute.For<IProfilingApiClient>();
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

            IJobsApiClient jobsApiClient = CreateJobsApiClient();
            jobsApiClient
                .GetJobById(Arg.Is(jobId))
                .Returns(new ApiResponse<JobViewModel>(HttpStatusCode.OK, new JobViewModel { Id = jobId }));

            PublishedResultsService service = CreateResultsService(
                logger: logger,
                publishedProviderResultsRepository: publishedProviderResultsRepository,
                profilingApiClient: providerProfilingRepository,
                specificationsRepository: specificationsRepository,
                jobsApiClient: jobsApiClient);

            string json = JsonConvert.SerializeObject(requestModel);

            Message message = new Message(Encoding.UTF8.GetBytes(json));
            message.UserProperties["specification-id"] = specificationId;
            message.UserProperties["jobId"] = jobId;

            // Act
            Func<Task> test = async () => await service.FetchProviderProfile(message);

            // Assert
            test
                .Should()
                .ThrowExactly<RetriableException>()
                .Which
                .Message
                .Should()
                .Be($"Failed to obtain profiling periods for provider: {result.ProviderId} and period: {result.FundingPeriod.Name}. Status Code = 'InternalServerError'");
        }

        [TestMethod]
        public async Task FetchProviderProfile_GivenFetchProviderProfileSucceedsAndMajorMinorIsEnabled_UpdatesPublishedProviderResult()
        {
            // Arrange
            int majorVersion = 3;
            int minorVersion = 42;

            PublishedProviderResult result = CreatePublishedProviderResults().First();
            result.SpecificationId = specificationId;
            result.FundingStreamResult.AllocationLineResult.Current.Status = AllocationLineStatus.Approved;
            result.FundingStreamResult.AllocationLineResult.Current.Major = majorVersion;
            result.FundingStreamResult.AllocationLineResult.Current.Minor = minorVersion;

            IEnumerable<FetchProviderProfilingMessageItem> requestModel = CreateProfilingMessageItems();
            requestModel.First().ProviderId = result.ProviderId;
            requestModel.First().AllocationLineResultId = result.Id;

            ValidatedApiResponse<ProviderProfilingResponseModel> profileResponse = new ValidatedApiResponse<ProviderProfilingResponseModel>(HttpStatusCode.OK, new ProviderProfilingResponseModel()
            {
                DeliveryProfilePeriods = new List<Common.ApiClient.Profiling.Models.ProfilingPeriod>
                 {
                    new Common.ApiClient.Profiling.Models.ProfilingPeriod { Period = "Oct", Occurrence = 1, Year = 2018, Type = "CalendarMonth", Value = 82190.0M, DistributionPeriod = "2018-2019" },
                    new Common.ApiClient.Profiling.Models.ProfilingPeriod { Period = "Apr", Occurrence = 1, Year = 2019, Type = "CalendarMonth", Value = 82190.0M, DistributionPeriod = "2018-2019" }
                 }
            });

            ILogger logger = Substitute.For<ILogger>();
            IPublishedProviderResultsRepository publishedProviderResultsRepository = Substitute.For<IPublishedProviderResultsRepository>();
            publishedProviderResultsRepository
                .GetPublishedProviderResultForId(Arg.Is(result.Id), Arg.Is(result.ProviderId))
                .Returns(result);
            IProfilingApiClient providerProfilingRepository = Substitute.For<IProfilingApiClient>();
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

            IJobsApiClient jobsApiClient = CreateJobsApiClient();
            jobsApiClient
                .GetJobById(Arg.Is(jobId))
                .Returns(new ApiResponse<JobViewModel>(HttpStatusCode.OK, new JobViewModel { Id = jobId }));

            PublishedResultsService service = CreateResultsService(logger: logger, publishedProviderResultsRepository: publishedProviderResultsRepository,
                profilingApiClient: providerProfilingRepository, specificationsRepository: specificationsRepository, allocationNotificationFeedSearchRepository: feedsSearchRepository,
                featureToggle: featureToggler, jobsApiClient: jobsApiClient);

            string json = JsonConvert.SerializeObject(requestModel);

            Message message = new Message(Encoding.UTF8.GetBytes(json));
            message.UserProperties["specification-id"] = specificationId;
            message.UserProperties["jobId"] = jobId;

            // Act
            await service.FetchProviderProfile(message);

            // Assert
            result
                .FundingStreamResult.AllocationLineResult.Current.ProfilingPeriods
                .Should()
                .BeEquivalentTo(profileResponse.Content.DeliveryProfilePeriods, "Profile Periods should be copied onto Published Provider Result");

            IEnumerable<PublishedProviderResult> toBeSavedResults = new List<PublishedProviderResult> { result };

            await publishedProviderResultsRepository.Received(1).SavePublishedResults(Arg.Is<IEnumerable<PublishedProviderResult>>(savedResults => toBeSavedResults.SequenceEqual(savedResults)));

            await feedsSearchRepository.Received(1).Index(Arg.Is<IEnumerable<AllocationNotificationFeedIndex>>(m => 
                m.Count() == 1 
                && m.First().MajorVersion == majorVersion
                && m.First().MinorVersion == minorVersion));

            await providerProfilingRepository.Received(1).GetProviderProfilePeriods(Arg.Is<ProviderProfilingRequestModel>(m =>
                m.AllocationValueByDistributionPeriod.First().AllocationValue == 50
            ));
        }

        [TestMethod]
        public async Task FetchProviderProfile_GivenFetchProviderProfileSucceeds_UpdatesPublishedProviderResult()
        {
            // Arrange
            int major = 3;
            int minor = 9;
            string feedIndexId = "feed-index-id-134567u65";

            PublishedProviderResult result = CreatePublishedProviderResults().First();
            result.SpecificationId = specificationId;
            result.FundingStreamResult.AllocationLineResult.Current.Status = AllocationLineStatus.Approved;
            result.FundingStreamResult.AllocationLineResult.Current.Major = major;
            result.FundingStreamResult.AllocationLineResult.Current.Minor = minor;
            result.FundingStreamResult.AllocationLineResult.Current.FeedIndexId = feedIndexId;

            IEnumerable<FetchProviderProfilingMessageItem> requestModel = CreateProfilingMessageItems();
            requestModel.First().ProviderId = result.ProviderId;
            requestModel.First().AllocationLineResultId = result.Id;

            ValidatedApiResponse<ProviderProfilingResponseModel> profileResponse = new ValidatedApiResponse<ProviderProfilingResponseModel>(HttpStatusCode.OK,
                new ProviderProfilingResponseModel
                {
                    DeliveryProfilePeriods = new List<Common.ApiClient.Profiling.Models.ProfilingPeriod>
                     {
                        new Common.ApiClient.Profiling.Models.ProfilingPeriod
                        {
                            Period = "October",
                            Occurrence = 1,
                            Year = 2018,
                            Type = "CalendarMonth",
                            Value = 82190.0M,
                            DistributionPeriod = "2018-2019"
                        },
                        new Common.ApiClient.Profiling.Models.ProfilingPeriod
                        {
                            Period = "April",
                            Occurrence = 1,
                            Year = 2019,
                            Type = "CalendarMonth",
                            Value = 82190.0M,
                            DistributionPeriod = "2018-2019"
                        }
                     }
                });

            ILogger logger = Substitute.For<ILogger>();

            IPublishedProviderResultsRepository publishedProviderResultsRepository = Substitute.For<IPublishedProviderResultsRepository>();
            publishedProviderResultsRepository
                .GetPublishedProviderResultForId(Arg.Is(result.Id), Arg.Is(result.ProviderId))
                .Returns(result);

            IProfilingApiClient providerProfilingRepository = Substitute.For<IProfilingApiClient>();
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

            IJobsApiClient jobsApiClient = CreateJobsApiClient();
            jobsApiClient
                .GetJobById(Arg.Is(jobId))
                .Returns(new ApiResponse<JobViewModel>(HttpStatusCode.OK, new JobViewModel { Id = jobId }));

            PublishedResultsService service = CreateResultsService(
                logger: logger,
                publishedProviderResultsRepository: publishedProviderResultsRepository,
                profilingApiClient: providerProfilingRepository,
                specificationsRepository: specificationsRepository,
                allocationNotificationFeedSearchRepository: feedsSearchRepository,
                jobsApiClient: jobsApiClient);

            string json = JsonConvert.SerializeObject(requestModel);

            Message message = new Message(Encoding.UTF8.GetBytes(json));
            message.UserProperties["specification-id"] = specificationId;
            message.UserProperties["jobId"] = jobId;

            // Act
            await service.FetchProviderProfile(message);

            // Assert
            result.FundingStreamResult.AllocationLineResult.Current.ProfilingPeriods
                .Should()
                .BeEquivalentTo(profileResponse.Content.DeliveryProfilePeriods, "Profile Periods should be copied onto Published Provider Result");

            IEnumerable<PublishedProviderResult> toBeSavedResults = new List<PublishedProviderResult> { result };
            await publishedProviderResultsRepository
                .Received(1)
                .SavePublishedResults(Arg.Is<IEnumerable<PublishedProviderResult>>(savedResults => toBeSavedResults.SequenceEqual(savedResults)));

            await feedsSearchRepository.Received(1).Index(Arg.Is<IEnumerable<AllocationNotificationFeedIndex>>(m =>
                m.Count() == 1 &&
                m.Single().MajorVersion == major &&
                m.Single().MinorVersion == minor &&
                m.Single().Title == "Allocation test allocation line 1 was Approved" &&
                m.Single().Id == feedIndexId));

            await providerProfilingRepository
                .Received(1)
                .GetProviderProfilePeriods(Arg.Is<ProviderProfilingRequestModel>(m =>
                    m.AllocationValueByDistributionPeriod.First().AllocationValue == 50));
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

            ValidatedApiResponse<ProviderProfilingResponseModel> profileResponse1 = new ValidatedApiResponse<ProviderProfilingResponseModel>(HttpStatusCode.OK, new ProviderProfilingResponseModel
            {
                DeliveryProfilePeriods = new List<Common.ApiClient.Profiling.Models.ProfilingPeriod>
                 {
                    new Common.ApiClient.Profiling.Models.ProfilingPeriod { Period = "Oct", Occurrence = 1, Year = 2018, Type = "CalendarMonth", Value = 82190.0M, DistributionPeriod = "2018-2019" },
                    new Common.ApiClient.Profiling.Models.ProfilingPeriod { Period = "Apr", Occurrence = 1, Year = 2019, Type = "CalendarMonth", Value = 82190.0M, DistributionPeriod = "2018-2019" }
                 },
                FinancialEnvelopes = new List<Common.ApiClient.Profiling.Models.FinancialEnvelope>
                 {
                    new Common.ApiClient.Profiling.Models.FinancialEnvelope {  MonthStart = Month.April, YearStart = 2018, MonthEnd = Month.March, YearEnd = 2019, Value = 164380M  },
                 }
            });

            ValidatedApiResponse<ProviderProfilingResponseModel> profileResponse2 = new ValidatedApiResponse<ProviderProfilingResponseModel>(HttpStatusCode.OK, new ProviderProfilingResponseModel
            {
                DeliveryProfilePeriods = new List<Common.ApiClient.Profiling.Models.ProfilingPeriod>
                 {
                    new Common.ApiClient.Profiling.Models.ProfilingPeriod { Period = "Oct", Occurrence = 1, Year = 2018, Type = "CalendarMonth", Value = 52190.0M, DistributionPeriod = "2018-2019" },
                    new Common.ApiClient.Profiling.Models.ProfilingPeriod { Period = "Apr", Occurrence = 1, Year = 2019, Type = "CalendarMonth", Value = 52190.0M, DistributionPeriod = "2018-2019" }
                 },
                FinancialEnvelopes = new List<Common.ApiClient.Profiling.Models.FinancialEnvelope>
                 {
                    new Common.ApiClient.Profiling.Models.FinancialEnvelope {  MonthStart = Month.April, YearStart = 2018, MonthEnd = Month.March, YearEnd = 2019, Value = 104380M  },
                 }
            });

            ValidatedApiResponse<ProviderProfilingResponseModel> profileResponse3 = new ValidatedApiResponse<ProviderProfilingResponseModel>(HttpStatusCode.OK, new ProviderProfilingResponseModel
            {
                DeliveryProfilePeriods = new List<Common.ApiClient.Profiling.Models.ProfilingPeriod>
                 {
                    new Common.ApiClient.Profiling.Models.ProfilingPeriod { Period = "Oct", Occurrence = 1, Year = 2018, Type = "CalendarMonth", Value = 32190.0M, DistributionPeriod = "2018-2019" },
                    new Common.ApiClient.Profiling.Models.ProfilingPeriod { Period = "Apr", Occurrence = 1, Year = 2019, Type = "CalendarMonth", Value = 32190.0M, DistributionPeriod = "2018-2019" }
                 },
                FinancialEnvelopes = new List<Common.ApiClient.Profiling.Models.FinancialEnvelope>
                 {
                    new Common.ApiClient.Profiling.Models.FinancialEnvelope {  MonthStart = Month.April, YearStart = 2018, MonthEnd = Month.March, YearEnd = 2019, Value = 64380M  },
                 }
            });

            ILogger logger = Substitute.For<ILogger>();
            IPublishedProviderResultsRepository publishedProviderResultsRepository = Substitute.For<IPublishedProviderResultsRepository>();
            publishedProviderResultsRepository
                .GetPublishedProviderResultForId(Arg.Any<string>(), Arg.Any<string>())
                .Returns(results.ElementAt(0), results.ElementAt(1), results.ElementAt(2));

            IProfilingApiClient providerProfilingRepository = Substitute.For<IProfilingApiClient>();
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

            IJobsApiClient jobsApiClient = CreateJobsApiClient();
            jobsApiClient
                .GetJobById(Arg.Is(jobId))
                .Returns(new ApiResponse<JobViewModel>(HttpStatusCode.OK, new JobViewModel { Id = jobId }));

            PublishedResultsService service = CreateResultsService(
                logger: logger,
                publishedProviderResultsRepository: publishedProviderResultsRepository,
                profilingApiClient: providerProfilingRepository,
                specificationsRepository: specificationsRepository,
                allocationNotificationFeedSearchRepository: feedsSearchRepository,
                jobsApiClient: jobsApiClient);

            string json = JsonConvert.SerializeObject(requestModel);

            Message message = new Message(Encoding.UTF8.GetBytes(json));
            message.UserProperties["specification-id"] = specificationId;
            message.UserProperties["jobId"] = jobId;

            // Act
            await service.FetchProviderProfile(message);

            // Assert
            results.ElementAt(0).FundingStreamResult.AllocationLineResult.Current.ProfilingPeriods.Should().NotBeNullOrEmpty();
            results.ElementAt(1).FundingStreamResult.AllocationLineResult.Current.ProfilingPeriods.Should().NotBeNullOrEmpty();
            results.ElementAt(2).FundingStreamResult.AllocationLineResult.Current.ProfilingPeriods.Should().NotBeNullOrEmpty();

            results.ElementAt(0).FundingStreamResult.AllocationLineResult.Current.FinancialEnvelopes.Should().NotBeNullOrEmpty();
            results.ElementAt(1).FundingStreamResult.AllocationLineResult.Current.FinancialEnvelopes.Should().NotBeNullOrEmpty();
            results.ElementAt(2).FundingStreamResult.AllocationLineResult.Current.FinancialEnvelopes.Should().NotBeNullOrEmpty();

            results.ElementAt(0).FundingStreamResult.AllocationLineResult.Current.FinancialEnvelopes.Should().HaveCount(1);
            results.ElementAt(1).FundingStreamResult.AllocationLineResult.Current.FinancialEnvelopes.Should().HaveCount(1);
            results.ElementAt(2).FundingStreamResult.AllocationLineResult.Current.FinancialEnvelopes.Should().HaveCount(1);

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
        public async Task FetchProviderProfile_GivenFetchProviderWithBatchOf3ButOneFailsToProfile_DoesNotUpdateResults()
        {
            // Arrange
            IEnumerable<PublishedProviderResult> results = CreatePublishedProviderResultsWithDifferentProviders();

            foreach (PublishedProviderResult result in results)
            {
                result.SpecificationId = specificationId;
                result.FundingStreamResult.AllocationLineResult.Current.Status = AllocationLineStatus.Approved;
            }

            ValidatedApiResponse<ProviderProfilingResponseModel> profileResponse1 = new ValidatedApiResponse<ProviderProfilingResponseModel>(HttpStatusCode.OK, new ProviderProfilingResponseModel
            {
                DeliveryProfilePeriods = new List<Common.ApiClient.Profiling.Models.ProfilingPeriod>
                 {
                    new Common.ApiClient.Profiling.Models.ProfilingPeriod { Period = "Oct", Occurrence = 1, Year = 2018, Type = "CalendarMonth", Value = 82190.0M, DistributionPeriod = "2018-2019" },
                    new Common.ApiClient.Profiling.Models.ProfilingPeriod { Period = "Apr", Occurrence = 1, Year = 2019, Type = "CalendarMonth", Value = 82190.0M, DistributionPeriod = "2018-2019" }
                 }
            });

            ValidatedApiResponse<ProviderProfilingResponseModel> profileResponse2 = new ValidatedApiResponse<ProviderProfilingResponseModel>(HttpStatusCode.OK, new ProviderProfilingResponseModel
            {
                DeliveryProfilePeriods = new List<Common.ApiClient.Profiling.Models.ProfilingPeriod>
                 {
                    new Common.ApiClient.Profiling.Models.ProfilingPeriod { Period = "Oct", Occurrence = 1, Year = 2018, Type = "CalendarMonth", Value = 52190.0M, DistributionPeriod = "2018-2019" },
                    new Common.ApiClient.Profiling.Models.ProfilingPeriod { Period = "Apr", Occurrence = 1, Year = 2019, Type = "CalendarMonth", Value = 52190.0M, DistributionPeriod = "2018-2019" }
                 }
            });

            ILogger logger = Substitute.For<ILogger>();
            IPublishedProviderResultsRepository publishedProviderResultsRepository = Substitute.For<IPublishedProviderResultsRepository>();
            publishedProviderResultsRepository
                .GetPublishedProviderResultForId(Arg.Any<string>(), Arg.Any<string>())
                .Returns(results.ElementAt(0), results.ElementAt(1), results.ElementAt(2));

            IProfilingApiClient providerProfilingRepository = Substitute.For<IProfilingApiClient>();
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

            IJobsApiClient jobsApiClient = CreateJobsApiClient();
            jobsApiClient
                .GetJobById(Arg.Is(jobId))
                .Returns(new ApiResponse<JobViewModel>(HttpStatusCode.OK, new JobViewModel { Id = jobId }));

            PublishedResultsService service = CreateResultsService(logger: logger, publishedProviderResultsRepository: publishedProviderResultsRepository,
                profilingApiClient: providerProfilingRepository, specificationsRepository: specificationsRepository, allocationNotificationFeedSearchRepository: feedsSearchRepository,
                jobsApiClient: jobsApiClient);

            string json = JsonConvert.SerializeObject(requestModel);

            Message message = new Message(Encoding.UTF8.GetBytes(json));
            message.UserProperties["specification-id"] = specificationId;
            message.UserProperties["jobId"] = jobId;

            // Act
            Func<Task> test = () => service.FetchProviderProfile(message);

            // Assert
            test
               .Should()
               .ThrowExactly<RetriableException>()
               .Which
               .Message
               .Should()
               .NotBeNullOrWhiteSpace();

            await publishedProviderResultsRepository.DidNotReceive().SavePublishedResults(Arg.Any<IEnumerable<PublishedProviderResult>>());
            await feedsSearchRepository.DidNotReceive().Index(Arg.Any<IEnumerable<AllocationNotificationFeedIndex>>());
        }

        [TestMethod]
        public async Task FetchProviderProfile_GivenUseJobServiceToggleSet_CallsJobService()
        {
            // Arrange
            IEnumerable<PublishedProviderResult> results = CreatePublishedProviderResultsWithDifferentProviders();

            foreach (PublishedProviderResult result in results)
            {
                result.SpecificationId = specificationId;
                result.FundingStreamResult.AllocationLineResult.Current.Status = AllocationLineStatus.Approved;
            }

            ValidatedApiResponse<ProviderProfilingResponseModel> profileResponse1 = new ValidatedApiResponse<ProviderProfilingResponseModel>(HttpStatusCode.OK, new ProviderProfilingResponseModel
            {
                DeliveryProfilePeriods = new List<Common.ApiClient.Profiling.Models.ProfilingPeriod>
                 {
                    new Common.ApiClient.Profiling.Models.ProfilingPeriod { Period = "Oct", Occurrence = 1, Year = 2018, Type = "CalendarMonth", Value = 82190.0M, DistributionPeriod = "2018-2019" },
                    new Common.ApiClient.Profiling.Models.ProfilingPeriod { Period = "Apr", Occurrence = 1, Year = 2019, Type = "CalendarMonth", Value = 82190.0M, DistributionPeriod = "2018-2019" }
                 },
                FinancialEnvelopes = new List<Common.ApiClient.Profiling.Models.FinancialEnvelope>
                 {
                    new Common.ApiClient.Profiling.Models.FinancialEnvelope {  MonthStart = Month.April, YearStart = 2018, MonthEnd = Month.March, YearEnd = 2019, Value = 164380M  },
                 }
            });

            ValidatedApiResponse<ProviderProfilingResponseModel> profileResponse2 = new ValidatedApiResponse<ProviderProfilingResponseModel>(HttpStatusCode.OK, new ProviderProfilingResponseModel
            {
                DeliveryProfilePeriods = new List<Common.ApiClient.Profiling.Models.ProfilingPeriod>
                 {
                    new Common.ApiClient.Profiling.Models.ProfilingPeriod { Period = "Oct", Occurrence = 1, Year = 2018, Type = "CalendarMonth", Value = 52190.0M, DistributionPeriod = "2018-2019" },
                    new Common.ApiClient.Profiling.Models.ProfilingPeriod { Period = "Apr", Occurrence = 1, Year = 2019, Type = "CalendarMonth", Value = 52190.0M, DistributionPeriod = "2018-2019" }
                 },
                FinancialEnvelopes = new List<Common.ApiClient.Profiling.Models.FinancialEnvelope>
                 {
                    new Common.ApiClient.Profiling.Models.FinancialEnvelope {  MonthStart = Month.April, YearStart = 2018, MonthEnd = Month.March, YearEnd = 2019, Value = 104380M  },
                 }
            });

            ValidatedApiResponse<ProviderProfilingResponseModel> profileResponse3 = new ValidatedApiResponse<ProviderProfilingResponseModel>(HttpStatusCode.OK, new ProviderProfilingResponseModel
            {
                DeliveryProfilePeriods = new List<Common.ApiClient.Profiling.Models.ProfilingPeriod>
                 {
                    new Common.ApiClient.Profiling.Models.ProfilingPeriod { Period = "Oct", Occurrence = 1, Year = 2018, Type = "CalendarMonth", Value = 32190.0M, DistributionPeriod = "2018-2019" },
                    new Common.ApiClient.Profiling.Models.ProfilingPeriod { Period = "Apr", Occurrence = 1, Year = 2019, Type = "CalendarMonth", Value = 32190.0M, DistributionPeriod = "2018-2019" }
                 },
                FinancialEnvelopes = new List<Common.ApiClient.Profiling.Models.FinancialEnvelope>
                 {
                    new Common.ApiClient.Profiling.Models.FinancialEnvelope {  MonthStart = Month.April, YearStart = 2018, MonthEnd = Month.March, YearEnd = 2019, Value = 64380M  },
                 }
            });

            ILogger logger = Substitute.For<ILogger>();
            IPublishedProviderResultsRepository publishedProviderResultsRepository = Substitute.For<IPublishedProviderResultsRepository>();
            publishedProviderResultsRepository
                .GetPublishedProviderResultForId(Arg.Any<string>(), Arg.Any<string>())
                .Returns(results.ElementAt(0), results.ElementAt(1), results.ElementAt(2));

            IProfilingApiClient providerProfilingRepository = Substitute.For<IProfilingApiClient>();
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

            IJobsApiClient jobsApiClient = CreateJobsApiClient();
            jobsApiClient
                .GetJobById(Arg.Is(jobId))
                .Returns(new ApiResponse<JobViewModel>(HttpStatusCode.OK, new JobViewModel { Id = jobId }));

            PublishedResultsService service = CreateResultsService(
                logger: logger,
                publishedProviderResultsRepository: publishedProviderResultsRepository,
                profilingApiClient: providerProfilingRepository,
                specificationsRepository: specificationsRepository,
                allocationNotificationFeedSearchRepository: feedsSearchRepository,
                jobsApiClient: jobsApiClient);

            string json = JsonConvert.SerializeObject(requestModel);

            Message message = new Message(Encoding.UTF8.GetBytes(json));
            message.UserProperties["specification-id"] = specificationId;
            message.UserProperties["jobId"] = jobId;

            // Act
            await service.FetchProviderProfile(message);

            // Assert
            results.ElementAt(0).FundingStreamResult.AllocationLineResult.Current.ProfilingPeriods.Should().NotBeNullOrEmpty();
            results.ElementAt(1).FundingStreamResult.AllocationLineResult.Current.ProfilingPeriods.Should().NotBeNullOrEmpty();
            results.ElementAt(2).FundingStreamResult.AllocationLineResult.Current.ProfilingPeriods.Should().NotBeNullOrEmpty();

            results.ElementAt(0).FundingStreamResult.AllocationLineResult.Current.FinancialEnvelopes.Should().NotBeNullOrEmpty();
            results.ElementAt(1).FundingStreamResult.AllocationLineResult.Current.FinancialEnvelopes.Should().NotBeNullOrEmpty();
            results.ElementAt(2).FundingStreamResult.AllocationLineResult.Current.FinancialEnvelopes.Should().NotBeNullOrEmpty();

            results.ElementAt(0).FundingStreamResult.AllocationLineResult.Current.FinancialEnvelopes.Should().HaveCount(1);
            results.ElementAt(1).FundingStreamResult.AllocationLineResult.Current.FinancialEnvelopes.Should().HaveCount(1);
            results.ElementAt(2).FundingStreamResult.AllocationLineResult.Current.FinancialEnvelopes.Should().HaveCount(1);

            IEnumerable<PublishedProviderResult> toBeSavedResults = new List<PublishedProviderResult> { results.ElementAt(0), results.ElementAt(1), results.ElementAt(2) };

            await publishedProviderResultsRepository.Received(1).SavePublishedResults(Arg.Is<IEnumerable<PublishedProviderResult>>(m => m.Count() == 3));
            await feedsSearchRepository.Received(1).Index(Arg.Is<IEnumerable<AllocationNotificationFeedIndex>>(m => m.Count() == 3));
            await providerProfilingRepository.Received(1).GetProviderProfilePeriods(Arg.Is<ProviderProfilingRequestModel>(m =>
                m.AllocationValueByDistributionPeriod.First().AllocationValue == 50
            ));
            await providerProfilingRepository.Received(2).GetProviderProfilePeriods(Arg.Is<ProviderProfilingRequestModel>(m =>
                m.AllocationValueByDistributionPeriod.First().AllocationValue == 100
            ));

            await jobsApiClient
                .Received(1)
                .AddJobLog(Arg.Is(jobId), Arg.Is<JobLogUpdateModel>(l => l.CompletedSuccessfully == null && l.ItemsProcessed == 0));
            await jobsApiClient
                .Received(1)
                .AddJobLog(Arg.Is(jobId), Arg.Is<JobLogUpdateModel>(l => l.CompletedSuccessfully == true && l.ItemsProcessed == 3 && l.ItemsFailed == 0 && l.ItemsSucceeded == 3));
        }

        private static ProviderProfilingRequestModel CreateProviderProfilingRequestModel()
        {
            return new ProviderProfilingRequestModel
            {
                AllocationValueByDistributionPeriod = new List<Common.ApiClient.Profiling.Models.AllocationPeriodValue>
                    {
                        new Common.ApiClient.Profiling.Models.AllocationPeriodValue{ DistributionPeriod = "2018", AllocationValue = 23.3M}
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
