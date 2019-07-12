using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using CalculateFunding.Models.Providers;
using CalculateFunding.Models.Results;
using CalculateFunding.Models.Results.Search;
using CalculateFunding.Models.Specs;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Interfaces.ServiceBus;
using CalculateFunding.Services.Results.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.ServiceBus;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Serilog;

namespace CalculateFunding.Services.Results.UnitTests.Services
{
    public partial class PublishedResultsServiceTests
    {
        [TestMethod]
        public async Task ReIndexAllocationNotificationFeeds_GivenMessageWithUserDetails_LogsInitiated()
        {
            //Arrange
            string userName = "Joseph Bloggs";
            Message message = new Message();
            message.UserProperties["user-id"] = "123";
            message.UserProperties["user-name"] = userName;

            IEnumerable<PublishedProviderResult> results = Enumerable.Empty<PublishedProviderResult>();

            IPublishedProviderResultsRepository repository = CreatePublishedProviderResultsRepository();
            repository
                .GetAllNonHeldPublishedProviderResults()
                .Returns(results);

            ILogger logger = CreateLogger();

            PublishedResultsService resultsService = CreateResultsService(logger, publishedProviderResultsRepository: repository);

            //Act
            await resultsService.ReIndexAllocationNotificationFeeds(message);

            //Assert
            logger
                .Received(1)
                .Information($"{nameof(resultsService.ReIndexAllocationNotificationFeeds)} initiated by: '{userName}'");
        }

        [TestMethod]
        public async Task ReIndexAllocationNotificationFeeds_GivenNoPublishedProviderResultsFound_LogsWarning()
        {
            //Arrange
            Message message = new Message();

            IEnumerable<PublishedProviderResult> results = Enumerable.Empty<PublishedProviderResult>();

            IPublishedProviderResultsRepository repository = CreatePublishedProviderResultsRepository();
            repository
                .GetAllNonHeldPublishedProviderResults()
                .Returns(results);

            ILogger logger = CreateLogger();

            PublishedResultsService resultsService = CreateResultsService(logger, publishedProviderResultsRepository: repository);

            //Act
            await resultsService.ReIndexAllocationNotificationFeeds(message);

            //Assert
            logger
                .Received()
                .Warning(Arg.Is("No published provider results were found to index."));

            logger
              .Received(1)
              .Information($"{nameof(resultsService.ReIndexAllocationNotificationFeeds)} initiated by: 'system'");
        }

        [TestMethod]
        public async Task ReIndexAllocationNotificationFeeds_GivenPublishedProviderFoundButUpdatingIndexThrowsException_ReturnsInternalServerError()
        {
            //Arrange
            Message message = new Message();

            IEnumerable<PublishedProviderResult> results = CreatePublishedProviderResultsWithDifferentProviders();
            foreach (PublishedProviderResult result in results)
            {
                result.FundingStreamResult.AllocationLineResult.Current.Status = AllocationLineStatus.Approved;
                result.FundingStreamResult.AllocationLineResult.Current.ProfilingPeriods = new[] { new ProfilingPeriod() };
            }

            IPublishedProviderResultsRepository repository = CreatePublishedProviderResultsRepository();
            repository
                .GetAllNonHeldPublishedProviderResults()
                .Returns(results);

            IEnumerable<PublishedAllocationLineResultVersion> history = CreatePublishedProviderResultsWithDifferentProviders()
                .Select(m => m.FundingStreamResult.AllocationLineResult.Current);
            history.ElementAt(1).Status = AllocationLineStatus.Approved;
            history.ElementAt(1).Status = AllocationLineStatus.Published;

            foreach (var providerVersion in history.GroupBy(c => c.PublishedProviderResultId))
            {
                if (providerVersion.Any())
                {
                    IEnumerable<PublishedAllocationLineResultVersion> providerHistory = providerVersion.AsEnumerable();

                    string providerId = providerHistory.First().ProviderId;

                    providerId
                        .Should()
                        .NotBeNullOrWhiteSpace();

                    repository
                   .GetAllNonHeldPublishedProviderResultVersions(Arg.Is(providerVersion.Key), Arg.Is(providerId))
                   .Returns(providerHistory);
                }
            }

            ILogger logger = CreateLogger();

            ISearchRepository<AllocationNotificationFeedIndex> searchRepository = CreateAllocationNotificationFeedSearchRepository();
            searchRepository.When(x => x.Index(Arg.Any<IEnumerable<AllocationNotificationFeedIndex>>()))
                            .Do(x => { throw new Exception("Error indexing"); });

            SpecificationCurrentVersion specification = CreateSpecification("spec-1");

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetCurrentSpecificationById(Arg.Is("spec-1"))
                .Returns(specification);

            PublishedResultsService resultsService = CreateResultsService(
                logger,
                publishedProviderResultsRepository: repository,
                allocationNotificationFeedSearchRepository: searchRepository,
                specificationsRepository: specificationsRepository);

            //Act
            Func<Task> test = async () => await resultsService.ReIndexAllocationNotificationFeeds(message);

            //Assert
            test
                .Should()
                .ThrowExactly<RetriableException>();

            logger
                .Received()
                .Error(Arg.Any<Exception>(), Arg.Is("Failed to index allocation feeds"));

            await
                repository
                .Received(3)
                .GetAllNonHeldPublishedProviderResultVersions(Arg.Any<string>(), Arg.Any<string>());
        }

        [TestMethod]
        public async Task ReIndexAllocationNotificationFeeds_GivenPublishedProviderFoundButAllHeld_DoesNotIndexReturnsContentResult()
        {
            //Arrange
            Message message = new Message();

            IPublishedProviderResultsRepository repository = CreatePublishedProviderResultsRepository();
            repository
                .GetAllNonHeldPublishedProviderResults()
                .Returns(Enumerable.Empty<PublishedProviderResult>());

            ILogger logger = CreateLogger();

            ISearchRepository<AllocationNotificationFeedIndex> searchRepository = CreateAllocationNotificationFeedSearchRepository();

            SpecificationCurrentVersion specification = CreateSpecification(specificationId);

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetCurrentSpecificationById(Arg.Is("spec-1"))
                .Returns(specification);

            PublishedResultsService resultsService = CreateResultsService(logger, publishedProviderResultsRepository: repository,
                allocationNotificationFeedSearchRepository: searchRepository, specificationsRepository: specificationsRepository);

            //Act
            await resultsService.ReIndexAllocationNotificationFeeds(message);

            //Assert
            await
                searchRepository
                .DidNotReceive()
                .Index(Arg.Any<IEnumerable<AllocationNotificationFeedIndex>>());

            logger
                .Received(1)
                .Warning(Arg.Is("No published provider results were found to index."));

            await
                repository
                .Received(0)
                .GetAllNonHeldPublishedProviderResultVersions(Arg.Any<string>(), Arg.Any<string>());
        }

        [TestMethod]
        public async Task ReIndexAllocationNotificationFeeds_GivenPublishedProviderFound_IndexesAndReturnsNoContentResult()
        {
            //Arrange
            Message message = new Message();

            const string specificationId = "spec-1";

            IEnumerable<PublishedProviderResult> results = CreatePublishedProviderResultsWithDifferentProviders();

            foreach (PublishedProviderResult result in results)
            {
                result.FundingStreamResult.AllocationLineResult.Current.Status = AllocationLineStatus.Approved;
                result.FundingStreamResult.AllocationLineResult.Current.ProfilingPeriods = new[] { new ProfilingPeriod() };
                result.FundingStreamResult.AllocationLineResult.Current.FinancialEnvelopes = new[] { new FinancialEnvelope() };
                result.FundingStreamResult.AllocationLineResult.Current.Calculations = new[]
                {
                    new PublishedProviderCalculationResult { CalculationSpecification = new Common.Models.Reference ("calc-id-1", "calc1"), Policy = new PolicySummary("policy-id-1", "policy1", "desc")},
                    new PublishedProviderCalculationResult { CalculationSpecification = new Common.Models.Reference ("calc-id-2", "calc2"), Policy = new PolicySummary("policy-id-1", "policy1", "desc")},
                    new PublishedProviderCalculationResult { CalculationSpecification = new Common.Models.Reference ("calc-id-3", "calc3"), Policy = new PolicySummary("policy-id-2", "policy2", "desc")},
                };
            }

            IPublishedProviderResultsRepository repository = CreatePublishedProviderResultsRepository();
            repository
                .GetAllNonHeldPublishedProviderResults()
                .Returns(results);

            ILogger logger = CreateLogger();

            ISearchRepository<AllocationNotificationFeedIndex> searchRepository = CreateAllocationNotificationFeedSearchRepository();

            SpecificationCurrentVersion specification = CreateSpecification(specificationId);

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetCurrentSpecificationById(Arg.Is(specificationId))
                .Returns(specification);

            IEnumerable<PublishedAllocationLineResultVersion> history = results.Select(m => m.FundingStreamResult.AllocationLineResult.Current);
            history.ElementAt(0).Status = AllocationLineStatus.Approved;
            history.ElementAt(1).Status = AllocationLineStatus.Approved;
            history.ElementAt(2).Status = AllocationLineStatus.Published;

            foreach (var providerVersion in history.GroupBy(c => c.PublishedProviderResultId))
            {
                if (providerVersion.Any())
                {
                    IEnumerable<PublishedAllocationLineResultVersion> providerHistory = providerVersion.AsEnumerable();

                    string providerId = providerHistory.First().ProviderId;

                    providerId
                        .Should()
                        .NotBeNullOrWhiteSpace();

                    repository
                   .GetAllNonHeldPublishedProviderResultVersions(Arg.Is(providerVersion.Key), Arg.Is(providerId))
                   .Returns(providerHistory);
                }
            }


            IEnumerable<AllocationNotificationFeedIndex> resultsBeingSaved = null;
            await searchRepository
                .Index(Arg.Do<IEnumerable<AllocationNotificationFeedIndex>>(r => resultsBeingSaved = r));

            PublishedResultsService resultsService = CreateResultsService(
                logger,
                publishedProviderResultsRepository: repository,
                allocationNotificationFeedSearchRepository: searchRepository,
                specificationsRepository: specificationsRepository);

            //Act
            await resultsService.ReIndexAllocationNotificationFeeds(message);

            //Assert
            await
                searchRepository
                .Received(1)
                .Index(Arg.Is<List<AllocationNotificationFeedIndex>>(m => m.Count() == 3));

            await
                repository
                .Received(3)
                .GetAllNonHeldPublishedProviderResultVersions(Arg.Any<string>(), Arg.Any<string>());

            AllocationNotificationFeedIndex feedResult = resultsBeingSaved.FirstOrDefault(m => m.ProviderId == "1111");

            feedResult.ProviderId.Should().Be("1111");
            feedResult.Title.Should().Be("Allocation test allocation line 1 was Approved");
            feedResult.Summary.Should().Be("UKPRN: 1111, version 0.1");
            feedResult.DatePublished.HasValue.Should().Be(false);
            feedResult.FundingStreamId.Should().Be("fs-1");
            feedResult.FundingStreamName.Should().Be("funding stream 1");
            feedResult.FundingPeriodId.Should().Be("1819");
            feedResult.ProviderUkPrn.Should().Be("1111");
            feedResult.ProviderUpin.Should().Be("2222");
            feedResult.AllocationLineId.Should().Be("AAAAA");
            feedResult.AllocationLineName.Should().Be("test allocation line 1");
            feedResult.AllocationVersionNumber.Should().Be(1);
            feedResult.AllocationAmount.Should().Be(50.0);
            feedResult.ProviderProfiling.Should().Be("[{\"period\":null,\"occurrence\":0,\"periodYear\":0,\"periodType\":null,\"profileValue\":0.0,\"distributionPeriod\":null}]");
            feedResult.ProviderName.Should().Be("test provider name 1");
            feedResult.LaCode.Should().Be("77777");
            feedResult.Authority.Should().Be("London");
            feedResult.ProviderType.Should().Be("test type");
            feedResult.SubProviderType.Should().Be("test sub type");
            feedResult.EstablishmentNumber.Should().Be("es123");
            feedResult.FundingPeriodStartYear.Should().Be(DateTime.Now.Year);
            feedResult.FundingPeriodEndYear.Should().Be(DateTime.Now.Year + 1);
            feedResult.FundingStreamStartDay.Should().Be(1);
            feedResult.FundingStreamStartMonth.Should().Be(8);
            feedResult.FundingStreamEndDay.Should().Be(31);
            feedResult.FundingStreamEndMonth.Should().Be(7);
            feedResult.FundingStreamPeriodName.Should().Be("period-type 1");
            feedResult.FundingStreamPeriodId.Should().Be("pt1");
            feedResult.AllocationLineContractRequired.Should().Be(true);
            feedResult.AllocationLineFundingRoute.Should().Be("LA");
            feedResult.AllocationLineShortName.Should().Be("tal1");
            feedResult.PolicySummaries.Should().Be("[{\"policy\":{\"description\":\"test decscription\",\"parentPolicyId\":null,\"id\":\"policy-1\",\"name\":\"policy one\"},\"policies\":[{\"policy\":{\"description\":\"test decscription\",\"parentPolicyId\":null,\"id\":\"subpolicy-1\",\"name\":\"sub policy one\"},\"policies\":[],\"calculations\":[]}],\"calculations\":[]}]");
            feedResult.Calculations.Should().Be("[{\"calculationName\":\"calc1\",\"calculationDisplayName\":\"calc1\",\"calculationVersion\":0,\"calculationType\":\"Number\",\"calculationAmount\":null,\"allocationLineId\":\"AAAAA\",\"policyId\":\"policy-id-1\",\"policyName\":\"policy1\"},{\"calculationName\":\"calc2\",\"calculationDisplayName\":\"calc2\",\"calculationVersion\":0,\"calculationType\":\"Number\",\"calculationAmount\":null,\"allocationLineId\":\"AAAAA\",\"policyId\":\"policy-id-1\",\"policyName\":\"policy1\"},{\"calculationName\":\"calc3\",\"calculationDisplayName\":\"calc3\",\"calculationVersion\":0,\"calculationType\":\"Number\",\"calculationAmount\":null,\"allocationLineId\":\"AAAAA\",\"policyId\":\"policy-id-2\",\"policyName\":\"policy2\"}]");
        }

        [TestMethod]
        public async Task ReIndexAllocationNotificationFeeds_GivenPublishedProviderFoundAndMajorMinorFeatureToggleIsEnabled_IndexesAndReturnsNoContentResult()
        {
            //Arrange
            int major = 2;
            int minor = 19;
            Message message = new Message();

            const string specificationId = "spec-1";

            IEnumerable<PublishedProviderResult> results = CreatePublishedProviderResultsWithDifferentProviders();
            foreach (PublishedProviderResult result in results)
            {
                result.FundingStreamResult.AllocationLineResult.Current.Status = AllocationLineStatus.Approved;
                result.FundingStreamResult.AllocationLineResult.Current.ProfilingPeriods = new[] { new ProfilingPeriod() };
                result.FundingStreamResult.AllocationLineResult.Current.Major = major;
                result.FundingStreamResult.AllocationLineResult.Current.Minor = minor;
            }

            IPublishedProviderResultsRepository repository = CreatePublishedProviderResultsRepository();
            repository
                .GetAllNonHeldPublishedProviderResults()
                .Returns(results);

            ILogger logger = CreateLogger();

            ISearchRepository<AllocationNotificationFeedIndex> searchRepository = CreateAllocationNotificationFeedSearchRepository();

            SpecificationCurrentVersion specification = CreateSpecification(specificationId);

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetCurrentSpecificationById(Arg.Is(specificationId))
                .Returns(specification);

            IEnumerable<PublishedAllocationLineResultVersion> history = results.Select(m => m.FundingStreamResult.AllocationLineResult.Current);
            history.ElementAt(0).Status = AllocationLineStatus.Approved;
            history.ElementAt(1).Status = AllocationLineStatus.Approved;
            history.ElementAt(2).Status = AllocationLineStatus.Published;

            foreach (var providerVersion in history.GroupBy(c => c.PublishedProviderResultId))
            {
                if (providerVersion.Any())
                {
                    IEnumerable<PublishedAllocationLineResultVersion> providerHistory = providerVersion.AsEnumerable();

                    string providerId = providerHistory.First().ProviderId;

                    providerId
                        .Should()
                        .NotBeNullOrWhiteSpace();

                    repository
                   .GetAllNonHeldPublishedProviderResultVersions(Arg.Is(providerVersion.Key), Arg.Is(providerId))
                   .Returns(providerHistory);
                }
            }

            IEnumerable<AllocationNotificationFeedIndex> resultsBeingSaved = null;
            await searchRepository
                .Index(Arg.Do<IEnumerable<AllocationNotificationFeedIndex>>(r => resultsBeingSaved = r));

            PublishedResultsService resultsService = CreateResultsService(
                logger,
                publishedProviderResultsRepository: repository,
                allocationNotificationFeedSearchRepository: searchRepository,
                specificationsRepository: specificationsRepository);

            //Act
            await resultsService.ReIndexAllocationNotificationFeeds(message);

            await
                searchRepository
                .Received(1)
                .Index(Arg.Is<IEnumerable<AllocationNotificationFeedIndex>>(m => m.Count() == 3));

            AllocationNotificationFeedIndex feedResult = resultsBeingSaved.FirstOrDefault(m => m.ProviderId == "1111");

            feedResult.ProviderId.Should().Be("1111");
            feedResult.Title.Should().Be("Allocation test allocation line 1 was Approved");
            feedResult.Summary.Should().Be($"UKPRN: 1111, version {major}.{minor}");
            feedResult.DatePublished.HasValue.Should().Be(false);
            feedResult.FundingStreamId.Should().Be("fs-1");
            feedResult.FundingStreamName.Should().Be("funding stream 1");
            feedResult.FundingPeriodId.Should().Be("1819");
            feedResult.ProviderUkPrn.Should().Be("1111");
            feedResult.ProviderUpin.Should().Be("2222");
            feedResult.AllocationLineId.Should().Be("AAAAA");
            feedResult.AllocationLineName.Should().Be("test allocation line 1");
            feedResult.AllocationVersionNumber.Should().Be(1);
            feedResult.AllocationAmount.Should().Be(50.0);
            feedResult.ProviderProfiling.Should().Be("[{\"period\":null,\"occurrence\":0,\"periodYear\":0,\"periodType\":null,\"profileValue\":0.0,\"distributionPeriod\":null}]");
            feedResult.ProviderName.Should().Be("test provider name 1");
            feedResult.LaCode.Should().Be("77777");
            feedResult.Authority.Should().Be("London");
            feedResult.ProviderType.Should().Be("test type");
            feedResult.SubProviderType.Should().Be("test sub type");
            feedResult.EstablishmentNumber.Should().Be("es123");
            feedResult.FundingPeriodStartYear.Should().Be(DateTime.Now.Year);
            feedResult.FundingPeriodEndYear.Should().Be(DateTime.Now.Year + 1);
            feedResult.FundingStreamStartDay.Should().Be(1);
            feedResult.FundingStreamStartMonth.Should().Be(8);
            feedResult.FundingStreamEndDay.Should().Be(31);
            feedResult.FundingStreamEndMonth.Should().Be(7);
            feedResult.FundingStreamPeriodName.Should().Be("period-type 1");
            feedResult.FundingStreamPeriodId.Should().Be("pt1");
            feedResult.AllocationLineContractRequired.Should().Be(true);
            feedResult.AllocationLineFundingRoute.Should().Be("LA");
            feedResult.AllocationLineShortName.Should().Be("tal1");
        }

        [TestMethod]
        public async Task ReIndexAllocationNotificationFeeds_GivenResultHasBeenVaried_IndexesAndReturnsNoContentResult()
        {
            //Arrange
            Message message = new Message();

            const string specificationId = "spec-1";

            IEnumerable<PublishedProviderResult> results = CreatePublishedProviderResultsWithDifferentProviders();
            string[] predecessors = { "prov1", "prov2" };
            const string reasonForOpening = "Fresh Start";
            const string providerSuccessor = "prov4";
            const string reasonForClosure = "Closure";

            foreach (PublishedProviderResult result in results)
            {
                result.FundingStreamResult.AllocationLineResult.Current.Status = AllocationLineStatus.Approved;
                result.FundingStreamResult.AllocationLineResult.Current.ProfilingPeriods = new[] { new ProfilingPeriod() };
                result.FundingStreamResult.AllocationLineResult.Current.FinancialEnvelopes = new[] { new FinancialEnvelope() };
                result.FundingStreamResult.AllocationLineResult.Current.Calculations = new[]
                {
                    new PublishedProviderCalculationResult { CalculationSpecification = new Common.Models.Reference ("calc-id-1", "calc1"), Policy = new PolicySummary("policy-id-1", "policy1", "desc")},
                    new PublishedProviderCalculationResult { CalculationSpecification = new Common.Models.Reference ("calc-id-2", "calc2"), Policy = new PolicySummary("policy-id-1", "policy1", "desc")},
                    new PublishedProviderCalculationResult { CalculationSpecification = new Common.Models.Reference ("calc-id-3", "calc3"), Policy = new PolicySummary("policy-id-2", "policy2", "desc")},
                };
            }

            PublishedAllocationLineResult publishedAllocationLineResult = results.First().FundingStreamResult.AllocationLineResult;
            publishedAllocationLineResult.Current.Provider.ReasonEstablishmentOpened = reasonForOpening;
            publishedAllocationLineResult.HasResultBeenVaried = true;
            publishedAllocationLineResult.Current.Predecessors = predecessors;
            publishedAllocationLineResult.Current.VariationReasons = new[] { VariationReason.LegalNameFieldUpdated, VariationReason.LACodeFieldUpdated };
            publishedAllocationLineResult.Current.Provider.Successor = providerSuccessor;
            publishedAllocationLineResult.Current.Provider.ReasonEstablishmentClosed = reasonForClosure;

            IPublishedProviderResultsRepository repository = CreatePublishedProviderResultsRepository();
            repository
                .GetAllNonHeldPublishedProviderResults()
                .Returns(results);

            IEnumerable<PublishedAllocationLineResultVersion> history = results.Select(m => m.FundingStreamResult.AllocationLineResult.Current);
            history.ElementAt(0).Status = AllocationLineStatus.Approved;
            history.ElementAt(1).Status = AllocationLineStatus.Approved;
            history.ElementAt(2).Status = AllocationLineStatus.Published;


            foreach (var providerVersion in history.GroupBy(c => c.PublishedProviderResultId))
            {
                if (providerVersion.Any())
                {
                    IEnumerable<PublishedAllocationLineResultVersion> providerHistory = providerVersion.AsEnumerable();

                    string providerId = providerHistory.First().ProviderId;

                    providerId
                        .Should()
                        .NotBeNullOrWhiteSpace();

                    repository
                   .GetAllNonHeldPublishedProviderResultVersions(Arg.Is(providerVersion.Key), Arg.Is(providerId))
                   .Returns(providerHistory);
                }
            }

            ILogger logger = CreateLogger();

            ISearchRepository<AllocationNotificationFeedIndex> searchRepository = CreateAllocationNotificationFeedSearchRepository();

            SpecificationCurrentVersion specification = CreateSpecification(specificationId);

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetCurrentSpecificationById(Arg.Is(specificationId))
                .Returns(specification);

            PublishedResultsService resultsService = CreateResultsService(
                logger,
                publishedProviderResultsRepository: repository,
                allocationNotificationFeedSearchRepository: searchRepository,
                specificationsRepository: specificationsRepository);

            IEnumerable<AllocationNotificationFeedIndex> resultsBeingSaved = null;
            await searchRepository
                .Index(Arg.Do<IEnumerable<AllocationNotificationFeedIndex>>(r => resultsBeingSaved = r));

            //Act
            await resultsService.ReIndexAllocationNotificationFeeds(message);

            //Assert
            await
                searchRepository
                .Received(1)
                .Index(Arg.Is<IEnumerable<AllocationNotificationFeedIndex>>(m => m.Count() == 3));

            AllocationNotificationFeedIndex feedResult = resultsBeingSaved.FirstOrDefault(m => m.ProviderId == "1111" && !string.IsNullOrWhiteSpace(m.CloseReason));

            feedResult.ProviderId.Should().Be("1111");
            feedResult.Title.Should().Be("Allocation test allocation line 1 was Approved");
            feedResult.Summary.Should().Be("UKPRN: 1111, version 0.1");
            feedResult.DatePublished.HasValue.Should().Be(false);
            feedResult.FundingStreamId.Should().Be("fs-1");
            feedResult.FundingStreamName.Should().Be("funding stream 1");
            feedResult.FundingPeriodId.Should().Be("1819");
            feedResult.ProviderUkPrn.Should().Be("1111");
            feedResult.ProviderUpin.Should().Be("2222");
            feedResult.AllocationLineId.Should().Be("AAAAA");
            feedResult.AllocationLineName.Should().Be("test allocation line 1");
            feedResult.AllocationVersionNumber.Should().Be(1);
            feedResult.AllocationAmount.Should().Be(50.0);
            feedResult.ProviderProfiling.Should().Be("[{\"period\":null,\"occurrence\":0,\"periodYear\":0,\"periodType\":null,\"profileValue\":0.0,\"distributionPeriod\":null}]");
            feedResult.ProviderName.Should().Be("test provider name 1");
            feedResult.LaCode.Should().Be("77777");
            feedResult.Authority.Should().Be("London");
            feedResult.ProviderType.Should().Be("test type");
            feedResult.SubProviderType.Should().Be("test sub type");
            feedResult.EstablishmentNumber.Should().Be("es123");
            feedResult.FundingPeriodStartYear.Should().Be(DateTime.Now.Year);
            feedResult.FundingPeriodEndYear.Should().Be(DateTime.Now.Year + 1);
            feedResult.FundingStreamStartDay.Should().Be(1);
            feedResult.FundingStreamStartMonth.Should().Be(8);
            feedResult.FundingStreamEndDay.Should().Be(31);
            feedResult.FundingStreamEndMonth.Should().Be(7);
            feedResult.FundingStreamPeriodName.Should().Be("period-type 1");
            feedResult.FundingStreamPeriodId.Should().Be("pt1");
            feedResult.AllocationLineContractRequired.Should().Be(true);
            feedResult.AllocationLineFundingRoute.Should().Be("LA");
            feedResult.AllocationLineShortName.Should().Be("tal1");
            feedResult.PolicySummaries.Should().Be("[{\"policy\":{\"description\":\"test decscription\",\"parentPolicyId\":null,\"id\":\"policy-1\",\"name\":\"policy one\"},\"policies\":[{\"policy\":{\"description\":\"test decscription\",\"parentPolicyId\":null,\"id\":\"subpolicy-1\",\"name\":\"sub policy one\"},\"policies\":[],\"calculations\":[]}],\"calculations\":[]}]");
            feedResult.Calculations.Should().Be("[{\"calculationName\":\"calc1\",\"calculationDisplayName\":\"calc1\",\"calculationVersion\":0,\"calculationType\":\"Number\",\"calculationAmount\":null,\"allocationLineId\":\"AAAAA\",\"policyId\":\"policy-id-1\",\"policyName\":\"policy1\"},{\"calculationName\":\"calc2\",\"calculationDisplayName\":\"calc2\",\"calculationVersion\":0,\"calculationType\":\"Number\",\"calculationAmount\":null,\"allocationLineId\":\"AAAAA\",\"policyId\":\"policy-id-1\",\"policyName\":\"policy1\"},{\"calculationName\":\"calc3\",\"calculationDisplayName\":\"calc3\",\"calculationVersion\":0,\"calculationType\":\"Number\",\"calculationAmount\":null,\"allocationLineId\":\"AAAAA\",\"policyId\":\"policy-id-2\",\"policyName\":\"policy2\"}]");
            feedResult.OpenReason.Should().Be(reasonForOpening);
            feedResult.Predecessors.SequenceEqual(predecessors).Should().BeTrue();
            feedResult.VariationReasons.SequenceEqual(new[] { "LegalNameFieldUpdated", "LACodeFieldUpdated" }).Should().BeTrue();
            feedResult.Successors.SequenceEqual(new[] { providerSuccessor }).Should().BeTrue();
            feedResult.CloseReason.Should().Be(reasonForClosure);
        }

        [TestMethod]
        public async Task ReIndexAllocationNotificationFeeds_GivenResultHasProviderDataChange_AndHasNotBeenVariedInAnyOtherWay_IndexesAndReturnsNoContentResult()
        {
            //Arrange
            Message message = new Message();

            const string specificationId = "spec-1";

            IEnumerable<PublishedProviderResult> results = CreatePublishedProviderResultsWithDifferentProviders();

            foreach (PublishedProviderResult result in results)
            {
                result.FundingStreamResult.AllocationLineResult.Current.Status = AllocationLineStatus.Approved;
                result.FundingStreamResult.AllocationLineResult.Current.ProfilingPeriods = new[] { new ProfilingPeriod() };
                result.FundingStreamResult.AllocationLineResult.Current.FinancialEnvelopes = new[] { new FinancialEnvelope() };
                result.FundingStreamResult.AllocationLineResult.Current.Calculations = new[]
                {
                    new PublishedProviderCalculationResult { CalculationSpecification = new Common.Models.Reference ("calc-id-1", "calc1"), Policy = new PolicySummary("policy-id-1", "policy1", "desc")},
                    new PublishedProviderCalculationResult { CalculationSpecification = new Common.Models.Reference ("calc-id-2", "calc2"), Policy = new PolicySummary("policy-id-1", "policy1", "desc")},
                    new PublishedProviderCalculationResult { CalculationSpecification = new Common.Models.Reference ("calc-id-3", "calc3"), Policy = new PolicySummary("policy-id-2", "policy2", "desc")},
                };
            }

            PublishedAllocationLineResult publishedAllocationLineResult = results.First().FundingStreamResult.AllocationLineResult;
            publishedAllocationLineResult.HasResultBeenVaried = false;
            publishedAllocationLineResult.Current.VariationReasons = new[] { VariationReason.AuthorityFieldUpdated, VariationReason.NameFieldUpdated };

            IPublishedProviderResultsRepository repository = CreatePublishedProviderResultsRepository();
            repository
                .GetAllNonHeldPublishedProviderResults()
                .Returns(results);

            IEnumerable<PublishedAllocationLineResultVersion> history = results.Select(m => m.FundingStreamResult.AllocationLineResult.Current);
            history.ElementAt(0).Status = AllocationLineStatus.Approved;
            history.ElementAt(1).Status = AllocationLineStatus.Approved;
            history.ElementAt(2).Status = AllocationLineStatus.Published;

            foreach (var providerVersion in history.GroupBy(c => c.PublishedProviderResultId))
            {
                if (providerVersion.Any())
                {
                    IEnumerable<PublishedAllocationLineResultVersion> providerHistory = providerVersion.AsEnumerable();

                    string providerId = providerHistory.First().ProviderId;

                    providerId
                        .Should()
                        .NotBeNullOrWhiteSpace();

                    repository
                   .GetAllNonHeldPublishedProviderResultVersions(Arg.Is(providerVersion.Key), Arg.Is(providerId))
                   .Returns(providerHistory);
                }
            }

            ILogger logger = CreateLogger();

            ISearchRepository<AllocationNotificationFeedIndex> searchRepository = CreateAllocationNotificationFeedSearchRepository();

            SpecificationCurrentVersion specification = CreateSpecification(specificationId);

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetCurrentSpecificationById(Arg.Is(specificationId))
                .Returns(specification);

            IEnumerable<AllocationNotificationFeedIndex> resultsBeingSaved = null;
            await searchRepository
                .Index(Arg.Do<IEnumerable<AllocationNotificationFeedIndex>>(r => resultsBeingSaved = r));

            PublishedResultsService resultsService = CreateResultsService(
                logger,
                publishedProviderResultsRepository: repository,
                allocationNotificationFeedSearchRepository: searchRepository,
                specificationsRepository: specificationsRepository);

            //Act
            await resultsService.ReIndexAllocationNotificationFeeds(message);

            //Assert
            await
                searchRepository
                .Received(1)
                .Index(Arg.Is<IEnumerable<AllocationNotificationFeedIndex>>(m => m.Count() == 3));

            AllocationNotificationFeedIndex feedResult = resultsBeingSaved.FirstOrDefault(m => m.ProviderId == "1111");

            feedResult.ProviderId.Should().Be("1111");
            feedResult.Title.Should().Be("Allocation test allocation line 1 was Approved");
            feedResult.Summary.Should().Be("UKPRN: 1111, version 0.1");
            feedResult.DatePublished.HasValue.Should().Be(false);
            feedResult.FundingStreamId.Should().Be("fs-1");
            feedResult.FundingStreamName.Should().Be("funding stream 1");
            feedResult.FundingPeriodId.Should().Be("1819");
            feedResult.ProviderUkPrn.Should().Be("1111");
            feedResult.ProviderUpin.Should().Be("2222");
            feedResult.AllocationLineId.Should().Be("AAAAA");
            feedResult.AllocationLineName.Should().Be("test allocation line 1");
            feedResult.AllocationVersionNumber.Should().Be(1);
            feedResult.AllocationAmount.Should().Be(50.0);
            feedResult.ProviderProfiling.Should().Be("[{\"period\":null,\"occurrence\":0,\"periodYear\":0,\"periodType\":null,\"profileValue\":0.0,\"distributionPeriod\":null}]");
            feedResult.ProviderName.Should().Be("test provider name 1");
            feedResult.LaCode.Should().Be("77777");
            feedResult.Authority.Should().Be("London");
            feedResult.ProviderType.Should().Be("test type");
            feedResult.SubProviderType.Should().Be("test sub type");
            feedResult.EstablishmentNumber.Should().Be("es123");
            feedResult.FundingPeriodStartYear.Should().Be(DateTime.Now.Year);
            feedResult.FundingPeriodEndYear.Should().Be(DateTime.Now.Year + 1);
            feedResult.FundingStreamStartDay.Should().Be(1);
            feedResult.FundingStreamStartMonth.Should().Be(8);
            feedResult.FundingStreamEndDay.Should().Be(31);
            feedResult.FundingStreamEndMonth.Should().Be(7);
            feedResult.FundingStreamPeriodName.Should().Be("period-type 1");
            feedResult.FundingStreamPeriodId.Should().Be("pt1");
            feedResult.AllocationLineContractRequired.Should().Be(true);
            feedResult.AllocationLineFundingRoute.Should().Be("LA");
            feedResult.AllocationLineShortName.Should().Be("tal1");
            feedResult.PolicySummaries.Should().Be("[{\"policy\":{\"description\":\"test decscription\",\"parentPolicyId\":null,\"id\":\"policy-1\",\"name\":\"policy one\"},\"policies\":[{\"policy\":{\"description\":\"test decscription\",\"parentPolicyId\":null,\"id\":\"subpolicy-1\",\"name\":\"sub policy one\"},\"policies\":[],\"calculations\":[]}],\"calculations\":[]}]");
            feedResult.Calculations.Should().Be("[{\"calculationName\":\"calc1\",\"calculationDisplayName\":\"calc1\",\"calculationVersion\":0,\"calculationType\":\"Number\",\"calculationAmount\":null,\"allocationLineId\":\"AAAAA\",\"policyId\":\"policy-id-1\",\"policyName\":\"policy1\"},{\"calculationName\":\"calc2\",\"calculationDisplayName\":\"calc2\",\"calculationVersion\":0,\"calculationType\":\"Number\",\"calculationAmount\":null,\"allocationLineId\":\"AAAAA\",\"policyId\":\"policy-id-1\",\"policyName\":\"policy1\"},{\"calculationName\":\"calc3\",\"calculationDisplayName\":\"calc3\",\"calculationVersion\":0,\"calculationType\":\"Number\",\"calculationAmount\":null,\"allocationLineId\":\"AAAAA\",\"policyId\":\"policy-id-2\",\"policyName\":\"policy2\"}]");
            feedResult.OpenReason.Should().BeNull();
            feedResult.CloseReason.Should().BeNull();
            feedResult.Predecessors.Should().BeNull();
            feedResult.Successors.Should().BeNull();

        }

        [TestMethod]
        public async Task ReIndexAllocationNotificationFeeds_GivenPublishedProviderFoundAndMajorMinorFeatureToggleIsEnabledAndIsAllAllocationResultsVersionsInFeedIndexEnabled_IndexesAndReturnsNoContentResult()
        {
            //Arrange
            int major = 2;
            int minor = 7;
            Message message = new Message();

            const string specificationId = "spec-1";

            IEnumerable<PublishedProviderResult> results = CreatePublishedProviderResultsWithDifferentProviders();

            foreach (PublishedProviderResult result in results)
            {
                result.FundingStreamResult.AllocationLineResult.Current.Status = AllocationLineStatus.Approved;
                result.FundingStreamResult.AllocationLineResult.Current.ProfilingPeriods = new[] { new ProfilingPeriod() };
                result.FundingStreamResult.AllocationLineResult.Current.Major = major;
                result.FundingStreamResult.AllocationLineResult.Current.Minor = minor;
                result.FundingStreamResult.AllocationLineResult.Current.FeedIndexId = "feed-index-id";
            }

            IPublishedProviderResultsRepository repository = CreatePublishedProviderResultsRepository();
            repository
                .GetAllNonHeldPublishedProviderResults()
                .Returns(results);

            ILogger logger = CreateLogger();

            IEnumerable<PublishedAllocationLineResultVersion> history = results.Select(m => m.FundingStreamResult.AllocationLineResult.Current);
            history.ElementAt(0).Status = AllocationLineStatus.Approved;

            foreach (var providerVersion in history.GroupBy(c => c.PublishedProviderResultId))
            {
                if (providerVersion.Any())
                {
                    IEnumerable<PublishedAllocationLineResultVersion> providerHistory = providerVersion.AsEnumerable();

                    string providerId = providerHistory.First().ProviderId;

                    providerId
                        .Should()
                        .NotBeNullOrWhiteSpace();

                    repository
                   .GetAllNonHeldPublishedProviderResultVersions(Arg.Is(providerVersion.Key), Arg.Is(providerId))
                   .Returns(providerHistory);
                }
            }

            ISearchRepository<AllocationNotificationFeedIndex> searchRepository = CreateAllocationNotificationFeedSearchRepository();

            SpecificationCurrentVersion specification = CreateSpecification(specificationId);

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetCurrentSpecificationById(Arg.Is("spec-1"))
                .Returns(specification);

            IEnumerable<AllocationNotificationFeedIndex> resultsBeingSaved = null;
            await searchRepository.Index(Arg.Do<IEnumerable<AllocationNotificationFeedIndex>>(r => resultsBeingSaved = r));

            PublishedResultsService resultsService = CreateResultsService(
                logger,
                publishedProviderResultsRepository: repository,
                allocationNotificationFeedSearchRepository: searchRepository,
                specificationsRepository: specificationsRepository);

            //Act
            await resultsService.ReIndexAllocationNotificationFeeds(message);

            //Assert
            await
                searchRepository
                .Received(1)
                .Index(Arg.Is<IEnumerable<AllocationNotificationFeedIndex>>(m => m.Count() == 3));

            AllocationNotificationFeedIndex feedResult = resultsBeingSaved.FirstOrDefault(m => m.ProviderId == "1111");

            feedResult.ProviderId.Should().Be("1111");
            feedResult.Title.Should().Be("Allocation test allocation line 1 was Approved");
            feedResult.Summary.Should().Be($"UKPRN: 1111, version {major}.{minor}");
            feedResult.DatePublished.HasValue.Should().Be(false);
            feedResult.FundingStreamId.Should().Be("fs-1");
            feedResult.FundingStreamName.Should().Be("funding stream 1");
            feedResult.FundingPeriodId.Should().Be("1819");
            feedResult.ProviderUkPrn.Should().Be("1111");
            feedResult.ProviderUpin.Should().Be("2222");
            feedResult.AllocationLineId.Should().Be("AAAAA");
            feedResult.AllocationLineName.Should().Be("test allocation line 1");
            feedResult.AllocationVersionNumber.Should().Be(1);
            feedResult.AllocationAmount.Should().Be(50.0);
            feedResult.ProviderProfiling.Should().Be("[{\"period\":null,\"occurrence\":0,\"periodYear\":0,\"periodType\":null,\"profileValue\":0.0,\"distributionPeriod\":null}]");
            feedResult.ProviderName.Should().Be("test provider name 1");
            feedResult.LaCode.Should().Be("77777");
            feedResult.Authority.Should().Be("London");
            feedResult.ProviderType.Should().Be("test type");
            feedResult.SubProviderType.Should().Be("test sub type");
            feedResult.EstablishmentNumber.Should().Be("es123");
            feedResult.FundingPeriodStartYear.Should().Be(DateTime.Now.Year);
            feedResult.FundingPeriodEndYear.Should().Be(DateTime.Now.Year + 1);
            feedResult.FundingStreamStartDay.Should().Be(1);
            feedResult.FundingStreamStartMonth.Should().Be(8);
            feedResult.FundingStreamEndDay.Should().Be(31);
            feedResult.FundingStreamEndMonth.Should().Be(7);
            feedResult.FundingStreamPeriodName.Should().Be("period-type 1");
            feedResult.FundingStreamPeriodId.Should().Be("pt1");
            feedResult.AllocationLineContractRequired.Should().Be(true);
            feedResult.AllocationLineFundingRoute.Should().Be("LA");
            feedResult.MajorVersion.Should().Be(major);
            feedResult.MinorVersion.Should().Be(minor);
        }

        [TestMethod]
        public async Task ReIndexAllocationNotificationFeeds_GivenRequest_AddsServiceBusMessage()
        {
            //Arrange
            string userId = "1234";
            string userName = "Joseph Bloggs";
            ClaimsPrincipal principle = new ClaimsPrincipal(new[]
           {
                new ClaimsIdentity(new []{ new Claim(ClaimTypes.Sid, userId), new Claim(ClaimTypes.Name, userName) })
            });

            HttpContext context = Substitute.For<HttpContext>();
            context
                .User
                .Returns(principle);

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .HttpContext
                .Returns(context);

            IMessengerService messengerService = CreateMessengerService();

            PublishedResultsService resultsService = CreateResultsService(messengerService: messengerService);

            //Act
            IActionResult actionResult = await resultsService.ReIndexAllocationNotificationFeeds(request);

            //Assert
            actionResult
                .Should()
                .BeAssignableTo<NoContentResult>();

            await
            messengerService
                .Received(1)
                .SendToQueue(
                    Arg.Is(ServiceBusConstants.QueueNames.ReIndexAllocationNotificationFeedIndex),
                    Arg.Is(string.Empty),
                    Arg.Is<IDictionary<string, string>>(m => m["user-id"] == userId && m["user-name"] == userName));
        }
    }
}
