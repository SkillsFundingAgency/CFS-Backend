using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
                result.ProfilingPeriods = new[] { new ProfilingPeriod() };
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
        public async Task ReIndexAllocationNotificationFeeds_GivenPublishedProviderFoundBuAllHeld_DoesNotIndexReturnsContentResult()
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
        public async Task ReIndexAllocationNotificationFeeds_GivenPublishedProviderFound_IndexesAndreturnsNoContentResult()
        {
            //Arrange
            IEnumerable<PublishedProviderResult> results = CreatePublishedProviderResultsWithDifferentProviders();
            foreach (PublishedProviderResult result in results)
            {
                result.FundingStreamResult.AllocationLineResult.Current.Status = AllocationLineStatus.Approved;
                result.ProfilingPeriods = new[] { new ProfilingPeriod() };
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
                       m.First().FundingPeriodId == "Ay12345" &&
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
                       m.First().AllocationLineShortName == "tal1"
           ));
        }
    }
}
