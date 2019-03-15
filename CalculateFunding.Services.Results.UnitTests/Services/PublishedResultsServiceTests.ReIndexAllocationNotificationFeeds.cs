using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.FeatureToggles;
using CalculateFunding.Models.Providers;
using CalculateFunding.Models.Results;
using CalculateFunding.Models.Results.Search;
using CalculateFunding.Models.Specs;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Services.Results.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Serilog;

namespace CalculateFunding.Services.Results.Services
{
    public partial class PublishedResultsServiceTests
    {
        [TestMethod]
        public async Task ReIndexAllocationNotificationFeeds_GivenNoPublishedProviderResultsFound_LogsWarning()
        {
            //Arrange
            IEnumerable<PublishedProviderResult> results = Enumerable.Empty<PublishedProviderResult>();

            IPublishedProviderResultsRepository repository = CreatePublishedProviderResultsRepository();
            repository
                .GetAllNonHeldPublishedProviderResults()
                .Returns(results);

            ILogger logger = CreateLogger();

            PublishedResultsService resultsService = CreateResultsService(logger, publishedProviderResultsRepository: repository);

            //Act
            IActionResult actionResult = await resultsService.ReIndexAllocationNotificationFeeds();

            //Assert
            actionResult
                .Should()
                .BeAssignableTo<NoContentResult>();

            logger
                .Received()
                .Warning(Arg.Is("No published provider results were found to index."));
        }

        [TestMethod]
        public async Task ReIndexAllocationNotificationFeeds_GivenPublishedProviderFoundButUpdatingIndexThrowsException_ReturnsInternalServerError()
        {
            //Arrange
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

            IEnumerable<PublishedAllocationLineResultVersion> history = CreatePublishedProviderResultsWithDifferentProviders().Select(m => m.FundingStreamResult.AllocationLineResult.Current);
            history.ElementAt(1).Status = AllocationLineStatus.Approved;
            history.ElementAt(1).Status = AllocationLineStatus.Published;

            IVersionRepository<PublishedAllocationLineResultVersion> versionRepository = CreatePublishedProviderResultsVersionRepository();
            versionRepository
                .GetVersions(Arg.Any<string>(), Arg.Any<string>())
                .Returns(history);

            ILogger logger = CreateLogger();

            ISearchRepository<AllocationNotificationFeedIndex> searchRepository = CreateAllocationNotificationFeedSearchRepository();
            searchRepository.When(x => x.Index(Arg.Any<IEnumerable<AllocationNotificationFeedIndex>>()))
                            .Do(x => { throw new Exception("Error indexing"); });

            SpecificationCurrentVersion specification = CreateSpecification("spec-1");

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetCurrentSpecificationById(Arg.Is("spec-1"))
                .Returns(specification);

            PublishedResultsService resultsService = CreateResultsService(logger, publishedProviderResultsRepository: repository,
                allocationNotificationFeedSearchRepository: searchRepository, specificationsRepository: specificationsRepository, publishedProviderResultsVersionRepository: versionRepository);

            //Act
            IActionResult actionResult = await resultsService.ReIndexAllocationNotificationFeeds();

            //Assert
            actionResult
                .Should()
                .BeAssignableTo<InternalServerErrorResult>()
                .Which
                .Value
                .Should()
                .Be("Error indexing");

            logger
                .Received()
                .Error(Arg.Any<Exception>(), Arg.Is("Failed to index allocation feeds"));
        }

        [TestMethod]
        public async Task ReIndexAllocationNotificationFeeds_GivenPublishedProviderFoundButAllHeld_DoesNotIndexReturnsContentResult()
        {
            //Arrange
            IEnumerable<PublishedProviderResult> results = CreatePublishedProviderResultsWithDifferentProviders();

            IPublishedProviderResultsRepository repository = CreatePublishedProviderResultsRepository();
            repository
                .GetAllNonHeldPublishedProviderResults()
                .Returns(results);

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
            IActionResult actionResult = await resultsService.ReIndexAllocationNotificationFeeds();

            //Assert
            actionResult
                .Should()
                .BeAssignableTo<NoContentResult>();

            await
                searchRepository
                .DidNotReceive()
                .Index(Arg.Any<IEnumerable<AllocationNotificationFeedIndex>>());
        }

        [TestMethod]
        public async Task ReIndexAllocationNotificationFeeds_GivenPublishedProviderFound_IndexesAndReturnsNoContentResult()
        {
            //Arrange
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

            IVersionRepository<PublishedAllocationLineResultVersion> versionRepository = CreatePublishedProviderResultsVersionRepository();
            versionRepository
                .GetVersions(Arg.Any<string>(), Arg.Any<string>())
                .Returns(history);

            IEnumerable<AllocationNotificationFeedIndex> resultsBeingSaved = null;
            await searchRepository
                .Index(Arg.Do<IEnumerable<AllocationNotificationFeedIndex>>(r => resultsBeingSaved = r));

            PublishedResultsService resultsService = CreateResultsService(logger, publishedProviderResultsRepository: repository,
                allocationNotificationFeedSearchRepository: searchRepository, specificationsRepository: specificationsRepository, publishedProviderResultsVersionRepository: versionRepository);

            //Act
            IActionResult actionResult = await resultsService.ReIndexAllocationNotificationFeeds();

            //Assert
            actionResult
                .Should()
                .BeAssignableTo<NoContentResult>();

            await
                searchRepository
                .Received(1)
                .Index(Arg.Is<List<AllocationNotificationFeedIndex>>(m => m.Count() == 9));

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
            feedResult.AllocationAmount.Should().Be((double)50.0);
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
            const string specificationId = "spec-1";

            IEnumerable<PublishedProviderResult> results = CreatePublishedProviderResultsWithDifferentProviders();
            foreach (PublishedProviderResult result in results)
            {
                result.FundingStreamResult.AllocationLineResult.Current.Status = AllocationLineStatus.Approved;
                result.FundingStreamResult.AllocationLineResult.Current.ProfilingPeriods = new[] { new ProfilingPeriod() };
                result.FundingStreamResult.AllocationLineResult.Current.Major = 1;
                result.FundingStreamResult.AllocationLineResult.Current.Minor = 5;
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

            IVersionRepository<PublishedAllocationLineResultVersion> versionRepository = CreatePublishedProviderResultsVersionRepository();
            versionRepository
                .GetVersions(Arg.Any<string>(), Arg.Any<string>())
                .Returns(history);

            IFeatureToggle featureToggle = Substitute.For<IFeatureToggle>();
            featureToggle
                .IsAllocationLineMajorMinorVersioningEnabled()
                .Returns(true);

            IEnumerable<AllocationNotificationFeedIndex> resultsBeingSaved = null;
            await searchRepository
                .Index(Arg.Do<IEnumerable<AllocationNotificationFeedIndex>>(r => resultsBeingSaved = r));

            PublishedResultsService resultsService = CreateResultsService(logger, publishedProviderResultsRepository: repository,
                allocationNotificationFeedSearchRepository: searchRepository, specificationsRepository: specificationsRepository, 
                featureToggle: featureToggle, publishedProviderResultsVersionRepository: versionRepository);

            //Act
            IActionResult actionResult = await resultsService.ReIndexAllocationNotificationFeeds();

            //Assert
            actionResult
                .Should()
                .BeAssignableTo<NoContentResult>();

            await
                searchRepository
                .Received(1)
                .Index(Arg.Is<IEnumerable<AllocationNotificationFeedIndex>>(m => m.Count() == 9));

            AllocationNotificationFeedIndex feedResult = resultsBeingSaved.FirstOrDefault(m => m.ProviderId == "1111");

            feedResult.ProviderId.Should().Be("1111");
            feedResult.Title.Should().Be("Allocation test allocation line 1 was Approved");
            feedResult.Summary.Should().Be("UKPRN: 1111, version 1.5");
            feedResult.DatePublished.HasValue.Should().Be(false);
            feedResult.FundingStreamId.Should().Be("fs-1");
            feedResult.FundingStreamName.Should().Be("funding stream 1");
            feedResult.FundingPeriodId.Should().Be("1819");
            feedResult.ProviderUkPrn.Should().Be("1111");
            feedResult.ProviderUpin.Should().Be("2222");
            feedResult.AllocationLineId.Should().Be("AAAAA");
            feedResult.AllocationLineName.Should().Be("test allocation line 1");
            feedResult.AllocationVersionNumber.Should().Be(1);
            feedResult.AllocationAmount.Should().Be((double)50.0);
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


            IVersionRepository<PublishedAllocationLineResultVersion> versionRepository = CreatePublishedProviderResultsVersionRepository();
            versionRepository
                .GetVersions(Arg.Any<string>(), Arg.Any<string>())
                .Returns(history);

            ILogger logger = CreateLogger();

            ISearchRepository<AllocationNotificationFeedIndex> searchRepository = CreateAllocationNotificationFeedSearchRepository();

            SpecificationCurrentVersion specification = CreateSpecification(specificationId);

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetCurrentSpecificationById(Arg.Is(specificationId))
                .Returns(specification);

            IFeatureToggle featureToggle = CreateFeatureToggle();
            featureToggle
                .IsProviderVariationsEnabled()
                .Returns(true);

            PublishedResultsService resultsService = CreateResultsService(logger, publishedProviderResultsRepository: repository,
                allocationNotificationFeedSearchRepository: searchRepository, specificationsRepository: specificationsRepository, featureToggle: featureToggle, publishedProviderResultsVersionRepository: versionRepository);

            IEnumerable<AllocationNotificationFeedIndex> resultsBeingSaved = null;
            await searchRepository
                .Index(Arg.Do<IEnumerable<AllocationNotificationFeedIndex>>(r => resultsBeingSaved = r));

            //Act
            IActionResult actionResult = await resultsService.ReIndexAllocationNotificationFeeds();

            //Assert
            actionResult
                .Should()
                .BeAssignableTo<NoContentResult>();

            await
                searchRepository
                .Received(1)
                .Index(Arg.Is<IEnumerable<AllocationNotificationFeedIndex>>(m => m.Count() == 9));

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
            feedResult.AllocationAmount.Should().Be((double)50.0);
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

            IVersionRepository<PublishedAllocationLineResultVersion> versionRepository = CreatePublishedProviderResultsVersionRepository();
            versionRepository
                .GetVersions(Arg.Any<string>(), Arg.Any<string>())
                .Returns(history);

            ILogger logger = CreateLogger();

            ISearchRepository<AllocationNotificationFeedIndex> searchRepository = CreateAllocationNotificationFeedSearchRepository();

            SpecificationCurrentVersion specification = CreateSpecification(specificationId);

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetCurrentSpecificationById(Arg.Is(specificationId))
                .Returns(specification);

            IFeatureToggle featureToggle = CreateFeatureToggle();
            featureToggle
                .IsProviderVariationsEnabled()
                .Returns(true);

            IEnumerable<AllocationNotificationFeedIndex> resultsBeingSaved = null;
            await searchRepository
                .Index(Arg.Do<IEnumerable<AllocationNotificationFeedIndex>>(r => resultsBeingSaved = r));

            PublishedResultsService resultsService = CreateResultsService(logger, publishedProviderResultsRepository: repository,
                allocationNotificationFeedSearchRepository: searchRepository, specificationsRepository: specificationsRepository, 
                featureToggle: featureToggle, publishedProviderResultsVersionRepository: versionRepository);

            //Act
            IActionResult actionResult = await resultsService.ReIndexAllocationNotificationFeeds();

            //Assert
            actionResult
                .Should()
                .BeAssignableTo<NoContentResult>();

            await
                searchRepository
                .Received(1)
                .Index(Arg.Is<IEnumerable<AllocationNotificationFeedIndex>>(m => m.Count() == 9));

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
            feedResult.AllocationAmount.Should().Be((double)50.0);
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
            const string specificationId = "spec-1";

            IEnumerable<PublishedProviderResult> results = CreatePublishedProviderResultsWithDifferentProviders();

            foreach (PublishedProviderResult result in results)
            {
                result.FundingStreamResult.AllocationLineResult.Current.Status = AllocationLineStatus.Approved;
                result.FundingStreamResult.AllocationLineResult.Current.ProfilingPeriods = new[] { new ProfilingPeriod() };
                result.FundingStreamResult.AllocationLineResult.Current.Major = 1;
                result.FundingStreamResult.AllocationLineResult.Current.Minor = 5;
                result.FundingStreamResult.AllocationLineResult.Current.FeedIndexId = "feed-index-id";
            }

            IPublishedProviderResultsRepository repository = CreatePublishedProviderResultsRepository();
            repository
                .GetAllNonHeldPublishedProviderResults()
                .Returns(results);

            ILogger logger = CreateLogger();

            IEnumerable<PublishedAllocationLineResultVersion> history = results.Select(m => m.FundingStreamResult.AllocationLineResult.Current);
            history.ElementAt(0).Status = AllocationLineStatus.Approved;

            IVersionRepository<PublishedAllocationLineResultVersion> versionRepository = CreatePublishedProviderResultsVersionRepository();
            versionRepository
                .GetVersions(Arg.Any<string>(), Arg.Any<string>())
                .Returns(history);

            ISearchRepository<AllocationNotificationFeedIndex> searchRepository = CreateAllocationNotificationFeedSearchRepository();

            SpecificationCurrentVersion specification = CreateSpecification(specificationId);

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetCurrentSpecificationById(Arg.Is("spec-1"))
                .Returns(specification);

            IFeatureToggle featureToggle = Substitute.For<IFeatureToggle>();
            featureToggle
                .IsAllocationLineMajorMinorVersioningEnabled()
                .Returns(true);
            featureToggle
                .IsAllAllocationResultsVersionsInFeedIndexEnabled()
                .Returns(true);

            IEnumerable<AllocationNotificationFeedIndex> resultsBeingSaved = null;
            await searchRepository
                .Index(Arg.Do<IEnumerable<AllocationNotificationFeedIndex>>(r => resultsBeingSaved = r));

            PublishedResultsService resultsService = CreateResultsService(logger, publishedProviderResultsRepository: repository,
                allocationNotificationFeedSearchRepository: searchRepository, specificationsRepository: specificationsRepository, 
                featureToggle: featureToggle, publishedProviderResultsVersionRepository: versionRepository);

            //Act
            IActionResult actionResult = await resultsService.ReIndexAllocationNotificationFeeds();

            //Assert
            actionResult
                .Should()
                .BeAssignableTo<NoContentResult>();

            await
                searchRepository
                .Received(1)
                .Index(Arg.Is<IEnumerable<AllocationNotificationFeedIndex>>(m => m.Count() == 9));

            AllocationNotificationFeedIndex feedResult = resultsBeingSaved.FirstOrDefault(m => m.ProviderId == "1111");

            feedResult.ProviderId.Should().Be("1111");
            feedResult.Title.Should().Be("Allocation test allocation line 1 was Approved");
            feedResult.Summary.Should().Be("UKPRN: 1111, version 1.5");
            feedResult.DatePublished.HasValue.Should().Be(false);
            feedResult.FundingStreamId.Should().Be("fs-1");
            feedResult.FundingStreamName.Should().Be("funding stream 1");
            feedResult.FundingPeriodId.Should().Be("1819");
            feedResult.ProviderUkPrn.Should().Be("1111");
            feedResult.ProviderUpin.Should().Be("2222");
            feedResult.AllocationLineId.Should().Be("AAAAA");
            feedResult.AllocationLineName.Should().Be("test allocation line 1");
            feedResult.AllocationVersionNumber.Should().Be(1);
            feedResult.AllocationAmount.Should().Be((double)50.0);
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
            feedResult.MajorVersion.Should().Be(1);
            feedResult.MinorVersion.Should().Be(5);
        }
    }
}
