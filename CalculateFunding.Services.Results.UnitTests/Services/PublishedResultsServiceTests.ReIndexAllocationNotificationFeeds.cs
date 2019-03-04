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

            ILogger logger = CreateLogger();

            ISearchRepository<AllocationNotificationFeedIndex> searchRepository = CreateAllocationNotificationFeedSearchRepository();
            searchRepository.When(x => x.Index(Arg.Any<IEnumerable<AllocationNotificationFeedIndex>>()))
                            .Do(x => { throw new Exception("Error indexing"); });

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
                .Received(1)
                .Index(Arg.Is<IEnumerable<AllocationNotificationFeedIndex>>(m => m.Count() == 3));

            await searchRepository
                   .Received(1)
                   .Index(Arg.Is<IEnumerable<AllocationNotificationFeedIndex>>(m =>
                       m.First().ProviderId == "1111" &&
                       m.First().Title == "test title 1" &&
                       m.First().Summary == "UKPRN: 1111, version 0.1" &&
                       m.First().DatePublished.HasValue == false &&
                       m.First().FundingStreamId == "fs-1" &&
                       m.First().FundingStreamName == "funding stream 1" &&
                       m.First().FundingPeriodId == "1819" &&
                       m.First().ProviderUkPrn == "1111" &&
                       m.First().ProviderUpin == "2222" &&
                       m.First().ProviderOpenDate.HasValue &&
                       m.First().AllocationLineId == "AAAAA" &&
                       m.First().AllocationLineName == "test allocation line 1" &&
                       m.First().AllocationVersionNumber == 1 &&
                       m.First().AllocationAmount == (double)50.0 &&
                       m.First().ProviderProfiling == "[{\"period\":null,\"occurrence\":0,\"periodYear\":0,\"periodType\":null,\"profileValue\":0.0,\"distributionPeriod\":null}]" &&
                       m.First().ProviderName == "test provider name 1" &&
                       m.First().LaCode == "77777" &&
                       m.First().Authority == "London" &&
                       m.First().ProviderType == "test type" &&
                       m.First().SubProviderType == "test sub type" &&
                       m.First().EstablishmentNumber == "es123" &&
                       m.First().FundingPeriodStartYear == DateTime.Now.Year &&
                       m.First().FundingPeriodEndYear == DateTime.Now.Year + 1 &&
                       m.First().FundingStreamStartDay == 1 &&
                       m.First().FundingStreamStartMonth == 8 &&
                       m.First().FundingStreamEndDay == 31 &&
                       m.First().FundingStreamEndMonth == 7 &&
                       m.First().FundingStreamPeriodName == "period-type 1" &&
                       m.First().FundingStreamPeriodId == "pt1" &&
                       m.First().AllocationLineContractRequired == true &&
                       m.First().AllocationLineFundingRoute == "LA" &&
                       m.First().AllocationLineShortName == "tal1" &&
                       m.First().PolicySummaries == "[{\"policy\":{\"description\":\"test decscription\",\"parentPolicyId\":null,\"id\":\"policy-1\",\"name\":\"policy one\"},\"policies\":[{\"policy\":{\"description\":\"test decscription\",\"parentPolicyId\":null,\"id\":\"subpolicy-1\",\"name\":\"sub policy one\"},\"policies\":[],\"calculations\":[]}],\"calculations\":[]}]" &&
                       m.First().Calculations == "[{\"calculationName\":\"calc1\",\"calculationDisplayName\":\"calc1\",\"calculationVersion\":0,\"calculationType\":\"Number\",\"calculationAmount\":null,\"allocationLineId\":\"AAAAA\",\"policyId\":\"policy-id-1\",\"policyName\":\"policy1\"},{\"calculationName\":\"calc2\",\"calculationDisplayName\":\"calc2\",\"calculationVersion\":0,\"calculationType\":\"Number\",\"calculationAmount\":null,\"allocationLineId\":\"AAAAA\",\"policyId\":\"policy-id-1\",\"policyName\":\"policy1\"},{\"calculationName\":\"calc3\",\"calculationDisplayName\":\"calc3\",\"calculationVersion\":0,\"calculationType\":\"Number\",\"calculationAmount\":null,\"allocationLineId\":\"AAAAA\",\"policyId\":\"policy-id-2\",\"policyName\":\"policy2\"}]"
           ));
        }

        [TestMethod]
        public async Task ReIndexAllocationNotificationFeeds_GivenPublishedProviderFoundAndMajorMinorFeatureToggleIsEnabled_IndexesAndReturnsNoContentResult()
        {
            //Arrange
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
                .GetCurrentSpecificationById(Arg.Is("spec-1"))
                .Returns(specification);

            IFeatureToggle featureToggle = Substitute.For<IFeatureToggle>();
            featureToggle
                .IsAllocationLineMajorMinorVersioningEnabled()
                .Returns(true);

            PublishedResultsService resultsService = CreateResultsService(logger, publishedProviderResultsRepository: repository,
                allocationNotificationFeedSearchRepository: searchRepository, specificationsRepository: specificationsRepository, featureToggle: featureToggle);

            //Act
            IActionResult actionResult = await resultsService.ReIndexAllocationNotificationFeeds();

            //Assert
            actionResult
                .Should()
                .BeAssignableTo<NoContentResult>();

            await
                searchRepository
                .Received(1)
                .Index(Arg.Is<IEnumerable<AllocationNotificationFeedIndex>>(m => m.Count() == 3));

            await searchRepository
                   .Received(1)
                   .Index(Arg.Is<IEnumerable<AllocationNotificationFeedIndex>>(m =>
                       m.First().ProviderId == "1111" &&
                       m.First().Title == "test title 1" &&
                       m.First().Summary == "UKPRN: 1111, version 1.5" &&
                       m.First().DatePublished.HasValue == false &&
                       m.First().FundingStreamId == "fs-1" &&
                       m.First().FundingStreamName == "funding stream 1" &&
                       m.First().FundingPeriodId == "1819" &&
                       m.First().ProviderUkPrn == "1111" &&
                       m.First().ProviderUpin == "2222" &&
                       m.First().ProviderOpenDate.HasValue &&
                       m.First().AllocationLineId == "AAAAA" &&
                       m.First().AllocationLineName == "test allocation line 1" &&
                       m.First().AllocationVersionNumber == 1 &&
                       m.First().AllocationAmount == (double)50.0 &&
                       m.First().ProviderProfiling == "[{\"period\":null,\"occurrence\":0,\"periodYear\":0,\"periodType\":null,\"profileValue\":0.0,\"distributionPeriod\":null}]" &&
                       m.First().ProviderName == "test provider name 1" &&
                       m.First().LaCode == "77777" &&
                       m.First().Authority == "London" &&
                       m.First().ProviderType == "test type" &&
                       m.First().SubProviderType == "test sub type" &&
                       m.First().EstablishmentNumber == "es123" &&
                       m.First().FundingPeriodStartYear == DateTime.Now.Year &&
                       m.First().FundingPeriodEndYear == DateTime.Now.Year + 1 &&
                       m.First().FundingStreamStartDay == 1 &&
                       m.First().FundingStreamStartMonth == 8 &&
                       m.First().FundingStreamEndDay == 31 &&
                       m.First().FundingStreamEndMonth == 7 &&
                       m.First().FundingStreamPeriodName == "period-type 1" &&
                       m.First().FundingStreamPeriodId == "pt1" &&
                       m.First().AllocationLineContractRequired == true &&
                       m.First().AllocationLineFundingRoute == "LA" &&
                       m.First().AllocationLineShortName == "tal1" &&
                       m.First().MajorVersion == 1 &&
                       m.First().MinorVersion == 5
           ));
        }

        [TestMethod]
        public async Task ReIndexAllocationNotificationFeeds_GivenResultHasBeenVaried_IndexesAndReturnsNoContentResult()
        {
            //Arrange
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

            ILogger logger = CreateLogger();

            ISearchRepository<AllocationNotificationFeedIndex> searchRepository = CreateAllocationNotificationFeedSearchRepository();

            SpecificationCurrentVersion specification = CreateSpecification(specificationId);

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetCurrentSpecificationById(Arg.Is("spec-1"))
                .Returns(specification);

            IFeatureToggle featureToggle = CreateFeatureToggle();
            featureToggle
                .IsProviderVariationsEnabled()
                .Returns(true);

            PublishedResultsService resultsService = CreateResultsService(logger, publishedProviderResultsRepository: repository,
                allocationNotificationFeedSearchRepository: searchRepository, specificationsRepository: specificationsRepository, featureToggle: featureToggle);

            //Act
            IActionResult actionResult = await resultsService.ReIndexAllocationNotificationFeeds();

            //Assert
            actionResult
                .Should()
                .BeAssignableTo<NoContentResult>();

            await
                searchRepository
                .Received(1)
                .Index(Arg.Is<IEnumerable<AllocationNotificationFeedIndex>>(m => m.Count() == 3));

            await searchRepository
                   .Received(1)
                   .Index(Arg.Is<IEnumerable<AllocationNotificationFeedIndex>>(m =>
                       m.First().ProviderId == "1111" &&
                       m.First().Title == "test title 1" &&
                       m.First().Summary == "UKPRN: 1111, version 0.1" &&
                       m.First().DatePublished.HasValue == false &&
                       m.First().FundingStreamId == "fs-1" &&
                       m.First().FundingStreamName == "funding stream 1" &&
                       m.First().FundingPeriodId == "1819" &&
                       m.First().ProviderUkPrn == "1111" &&
                       m.First().ProviderUpin == "2222" &&
                       m.First().ProviderOpenDate.HasValue &&
                       m.First().AllocationLineId == "AAAAA" &&
                       m.First().AllocationLineName == "test allocation line 1" &&
                       m.First().AllocationVersionNumber == 1 &&
                       m.First().AllocationAmount == (double)50.0 &&
                       m.First().ProviderProfiling == "[{\"period\":null,\"occurrence\":0,\"periodYear\":0,\"periodType\":null,\"profileValue\":0.0,\"distributionPeriod\":null}]" &&
                       m.First().ProviderName == "test provider name 1" &&
                       m.First().LaCode == "77777" &&
                       m.First().Authority == "London" &&
                       m.First().ProviderType == "test type" &&
                       m.First().SubProviderType == "test sub type" &&
                       m.First().EstablishmentNumber == "es123" &&
                       m.First().FundingPeriodStartYear == DateTime.Now.Year &&
                       m.First().FundingPeriodEndYear == DateTime.Now.Year + 1 &&
                       m.First().FundingStreamStartDay == 1 &&
                       m.First().FundingStreamStartMonth == 8 &&
                       m.First().FundingStreamEndDay == 31 &&
                       m.First().FundingStreamEndMonth == 7 &&
                       m.First().FundingStreamPeriodName == "period-type 1" &&
                       m.First().FundingStreamPeriodId == "pt1" &&
                       m.First().AllocationLineContractRequired == true &&
                       m.First().AllocationLineFundingRoute == "LA" &&
                       m.First().AllocationLineShortName == "tal1" &&
                       m.First().PolicySummaries == "[{\"policy\":{\"description\":\"test decscription\",\"parentPolicyId\":null,\"id\":\"policy-1\",\"name\":\"policy one\"},\"policies\":[{\"policy\":{\"description\":\"test decscription\",\"parentPolicyId\":null,\"id\":\"subpolicy-1\",\"name\":\"sub policy one\"},\"policies\":[],\"calculations\":[]}],\"calculations\":[]}]" &&
                       m.First().Calculations == "[{\"calculationName\":\"calc1\",\"calculationDisplayName\":\"calc1\",\"calculationVersion\":0,\"calculationType\":\"Number\",\"calculationAmount\":null,\"allocationLineId\":\"AAAAA\",\"policyId\":\"policy-id-1\",\"policyName\":\"policy1\"},{\"calculationName\":\"calc2\",\"calculationDisplayName\":\"calc2\",\"calculationVersion\":0,\"calculationType\":\"Number\",\"calculationAmount\":null,\"allocationLineId\":\"AAAAA\",\"policyId\":\"policy-id-1\",\"policyName\":\"policy1\"},{\"calculationName\":\"calc3\",\"calculationDisplayName\":\"calc3\",\"calculationVersion\":0,\"calculationType\":\"Number\",\"calculationAmount\":null,\"allocationLineId\":\"AAAAA\",\"policyId\":\"policy-id-2\",\"policyName\":\"policy2\"}]" &&
                       m.First().OpenReason == reasonForOpening &&
                       m.First().Predecessors.SequenceEqual(predecessors) &&
                       m.First().VariationReasons.SequenceEqual(new[] { "LegalNameFieldUpdated", "LACodeFieldUpdated" }) &&
                       m.First().Successors.SequenceEqual(new[] { providerSuccessor }) &&
                       m.First().CloseReason == reasonForClosure
           ));
        }

        [TestMethod]
        public async Task ReIndexAllocationNotificationFeeds_GivenResultHasProviderDataChange_AndHasNotBeenVariedInAnyOtherWay_IndexesAndReturnsNoContentResult()
        {
            //Arrange
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

            ILogger logger = CreateLogger();

            ISearchRepository<AllocationNotificationFeedIndex> searchRepository = CreateAllocationNotificationFeedSearchRepository();

            SpecificationCurrentVersion specification = CreateSpecification(specificationId);

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetCurrentSpecificationById(Arg.Is("spec-1"))
                .Returns(specification);

            IFeatureToggle featureToggle = CreateFeatureToggle();
            featureToggle
                .IsProviderVariationsEnabled()
                .Returns(true);

            PublishedResultsService resultsService = CreateResultsService(logger, publishedProviderResultsRepository: repository,
                allocationNotificationFeedSearchRepository: searchRepository, specificationsRepository: specificationsRepository, featureToggle: featureToggle);

            //Act
            IActionResult actionResult = await resultsService.ReIndexAllocationNotificationFeeds();

            //Assert
            actionResult
                .Should()
                .BeAssignableTo<NoContentResult>();

            await
                searchRepository
                .Received(1)
                .Index(Arg.Is<IEnumerable<AllocationNotificationFeedIndex>>(m => m.Count() == 3));

            await searchRepository
                   .Received(1)
                   .Index(Arg.Is<IEnumerable<AllocationNotificationFeedIndex>>(m =>
                       m.First().ProviderId == "1111" &&
                       m.First().Title == "test title 1" &&
                       m.First().Summary == "UKPRN: 1111, version 0.1" &&
                       m.First().DatePublished.HasValue == false &&
                       m.First().FundingStreamId == "fs-1" &&
                       m.First().FundingStreamName == "funding stream 1" &&
                       m.First().FundingPeriodId == "1819" &&
                       m.First().ProviderUkPrn == "1111" &&
                       m.First().ProviderUpin == "2222" &&
                       m.First().ProviderOpenDate.HasValue &&
                       m.First().AllocationLineId == "AAAAA" &&
                       m.First().AllocationLineName == "test allocation line 1" &&
                       m.First().AllocationVersionNumber == 1 &&
                       m.First().AllocationAmount == (double)50.0 &&
                       m.First().ProviderProfiling == "[{\"period\":null,\"occurrence\":0,\"periodYear\":0,\"periodType\":null,\"profileValue\":0.0,\"distributionPeriod\":null}]" &&
                       m.First().ProviderName == "test provider name 1" &&
                       m.First().LaCode == "77777" &&
                       m.First().Authority == "London" &&
                       m.First().ProviderType == "test type" &&
                       m.First().SubProviderType == "test sub type" &&
                       m.First().EstablishmentNumber == "es123" &&
                       m.First().FundingPeriodStartYear == DateTime.Now.Year &&
                       m.First().FundingPeriodEndYear == DateTime.Now.Year + 1 &&
                       m.First().FundingStreamStartDay == 1 &&
                       m.First().FundingStreamStartMonth == 8 &&
                       m.First().FundingStreamEndDay == 31 &&
                       m.First().FundingStreamEndMonth == 7 &&
                       m.First().FundingStreamPeriodName == "period-type 1" &&
                       m.First().FundingStreamPeriodId == "pt1" &&
                       m.First().AllocationLineContractRequired == true &&
                       m.First().AllocationLineFundingRoute == "LA" &&
                       m.First().AllocationLineShortName == "tal1" &&
                       m.First().PolicySummaries == "[{\"policy\":{\"description\":\"test decscription\",\"parentPolicyId\":null,\"id\":\"policy-1\",\"name\":\"policy one\"},\"policies\":[{\"policy\":{\"description\":\"test decscription\",\"parentPolicyId\":null,\"id\":\"subpolicy-1\",\"name\":\"sub policy one\"},\"policies\":[],\"calculations\":[]}],\"calculations\":[]}]" &&
                       m.First().Calculations == "[{\"calculationName\":\"calc1\",\"calculationDisplayName\":\"calc1\",\"calculationVersion\":0,\"calculationType\":\"Number\",\"calculationAmount\":null,\"allocationLineId\":\"AAAAA\",\"policyId\":\"policy-id-1\",\"policyName\":\"policy1\"},{\"calculationName\":\"calc2\",\"calculationDisplayName\":\"calc2\",\"calculationVersion\":0,\"calculationType\":\"Number\",\"calculationAmount\":null,\"allocationLineId\":\"AAAAA\",\"policyId\":\"policy-id-1\",\"policyName\":\"policy1\"},{\"calculationName\":\"calc3\",\"calculationDisplayName\":\"calc3\",\"calculationVersion\":0,\"calculationType\":\"Number\",\"calculationAmount\":null,\"allocationLineId\":\"AAAAA\",\"policyId\":\"policy-id-2\",\"policyName\":\"policy2\"}]" &&
                       m.First().OpenReason == null &&
                       m.First().Predecessors == null &&
                       m.First().VariationReasons.SequenceEqual(new[] { "AuthorityFieldUpdated", "NameFieldUpdated" }) &&
                       m.First().Successors == null &&
                       m.First().CloseReason == null
           ));
        }

        [TestMethod]
        public async Task ReIndexAllocationNotificationFeeds_GivenPublishedProviderFoundAndMajorMinorFeatureToggleIsEnabledAndIsAllAllocationResultsVersionsInFeedIndexEnabled_IndexesAndReturnsNoContentResult()
        {
            //Arrange
            IEnumerable<PublishedProviderResult> results = CreatePublishedProviderResultsWithDifferentProviders();
            foreach (PublishedProviderResult result in results)
            {
                result.FundingStreamResult.AllocationLineResult.Current.Status = AllocationLineStatus.Approved;
                result.FundingStreamResult.AllocationLineResult.Current.ProfilingPeriods = new[] { new ProfilingPeriod() };
                result.FundingStreamResult.AllocationLineResult.Current.Major = 1;
                result.FundingStreamResult.AllocationLineResult.Current.Minor = 5;
                result.FundingStreamResult.AllocationLineResult.Current.Title = "title";
                result.FundingStreamResult.AllocationLineResult.Current.FeedIndexId = "feed-index-id";
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
                .GetCurrentSpecificationById(Arg.Is("spec-1"))
                .Returns(specification);

            IFeatureToggle featureToggle = Substitute.For<IFeatureToggle>();
            featureToggle
                .IsAllocationLineMajorMinorVersioningEnabled()
                .Returns(true);
            featureToggle
                .IsAllAllocationResultsVersionsInFeedIndexEnabled()
                .Returns(true);

            PublishedResultsService resultsService = CreateResultsService(logger, publishedProviderResultsRepository: repository,
                allocationNotificationFeedSearchRepository: searchRepository, specificationsRepository: specificationsRepository, featureToggle: featureToggle);

            //Act
            IActionResult actionResult = await resultsService.ReIndexAllocationNotificationFeeds();

            //Assert
            actionResult
                .Should()
                .BeAssignableTo<NoContentResult>();

            await
                searchRepository
                .Received(1)
                .Index(Arg.Is<IEnumerable<AllocationNotificationFeedIndex>>(m => m.Count() == 3));

            await searchRepository
                   .Received(1)
                   .Index(Arg.Is<IEnumerable<AllocationNotificationFeedIndex>>(m =>
                       m.First().Id == "feed-index-id" &&
                       m.First().ProviderId == "1111" &&
                       m.First().Title == "title" &&
                       m.First().Summary == "UKPRN: 1111, version 1.5" &&
                       m.First().DatePublished.HasValue == false &&
                       m.First().FundingStreamId == "fs-1" &&
                       m.First().FundingStreamName == "funding stream 1" &&
                       m.First().FundingPeriodId == "1819" &&
                       m.First().ProviderUkPrn == "1111" &&
                       m.First().ProviderUpin == "2222" &&
                       m.First().ProviderOpenDate.HasValue &&
                       m.First().AllocationLineId == "AAAAA" &&
                       m.First().AllocationLineName == "test allocation line 1" &&
                       m.First().AllocationVersionNumber == 1 &&
                       m.First().AllocationAmount == (double)50.0 &&
                       m.First().ProviderProfiling == "[{\"period\":null,\"occurrence\":0,\"periodYear\":0,\"periodType\":null,\"profileValue\":0.0,\"distributionPeriod\":null}]" &&
                       m.First().ProviderName == "test provider name 1" &&
                       m.First().LaCode == "77777" &&
                       m.First().Authority == "London" &&
                       m.First().ProviderType == "test type" &&
                       m.First().SubProviderType == "test sub type" &&
                       m.First().EstablishmentNumber == "es123" &&
                       m.First().FundingPeriodStartYear == DateTime.Now.Year &&
                       m.First().FundingPeriodEndYear == DateTime.Now.Year + 1 &&
                       m.First().FundingStreamStartDay == 1 &&
                       m.First().FundingStreamStartMonth == 8 &&
                       m.First().FundingStreamEndDay == 31 &&
                       m.First().FundingStreamEndMonth == 7 &&
                       m.First().FundingStreamPeriodName == "period-type 1" &&
                       m.First().FundingStreamPeriodId == "pt1" &&
                       m.First().AllocationLineContractRequired == true &&
                       m.First().AllocationLineFundingRoute == "LA" &&
                       m.First().AllocationLineShortName == "tal1" &&
                       m.First().MajorVersion == 1 &&
                       m.First().MinorVersion == 5 &&
                       m.First().IsDeleted == false
           ));
        }
    }
}
