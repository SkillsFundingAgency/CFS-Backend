
using CalculateFunding.Models.Results;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Services.Results.Interfaces;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Results.Services
{
    public partial class ResultsServiceTests
    {
        [TestMethod]
        public async Task GetPublishedProviderResultWithHistoryByAllocationResultId_GivenResultNotFound_ResturnsNull()
        {
            //Arrange
            string allocationResultId = "12345";

            IPublishedProviderResultsRepository publishedProviderResultsRepository = CreatePublishedProviderResultsRepository();
            publishedProviderResultsRepository
                .GetPublishedProviderResultForIdInPublishedState(Arg.Is(allocationResultId))
                .Returns((PublishedProviderResult)null);

            ResultsService service = CreateResultsService(publishedProviderResultsRepository: publishedProviderResultsRepository);

            //Act
            PublishedProviderResultWithHistory result = await service.GetPublishedProviderResultWithHistoryByAllocationResultId(allocationResultId);

            //Assert
            result
                .Should()
                .BeNull();
        }

        [TestMethod]
        public async Task GetPublishedProviderResultWithHistoryByAllocationResultId_GivenResultFoundButNoHistory_ResturnsNull()
        {
            //Arrange
            string allocationResultId = "12345";

            PublishedProviderResult publishedProviderResult = new PublishedProviderResult();

            IPublishedProviderResultsRepository publishedProviderResultsRepository = CreatePublishedProviderResultsRepository();
            publishedProviderResultsRepository
                .GetPublishedProviderResultForIdInPublishedState(Arg.Is(allocationResultId))
                .Returns(publishedProviderResult);

            IVersionRepository<PublishedAllocationLineResultVersion> versionRepository = CreatePublishedProviderResultsVersionRepository();
            versionRepository
                .GetVersions(Arg.Is(allocationResultId))
                .Returns((IEnumerable<PublishedAllocationLineResultVersion>)null);

            ResultsService service = CreateResultsService(publishedProviderResultsRepository: publishedProviderResultsRepository, publishedProviderResultsVersionRepository: versionRepository);

            //Act
            PublishedProviderResultWithHistory result = await service.GetPublishedProviderResultWithHistoryByAllocationResultId(allocationResultId);

            //Assert
            result
                .Should()
                .BeNull();
        }

        [TestMethod]
        public async Task GetPublishedProviderResultWithHistoryByAllocationResultId_GivenResultAndHistory_ResturnsResult()
        {
            //Arrange
            string allocationResultId = "12345";

            PublishedProviderResult publishedProviderResult = new PublishedProviderResult
            {
                FundingStreamResult = new PublishedFundingStreamResult
                {
                    AllocationLineResult = new PublishedAllocationLineResult { }
                }
            };

            IEnumerable<PublishedAllocationLineResultVersion> history = new[]
            {
                 new PublishedAllocationLineResultVersion(),
                    new PublishedAllocationLineResultVersion(),
                    new PublishedAllocationLineResultVersion()
            };

            IPublishedProviderResultsRepository publishedProviderResultsRepository = CreatePublishedProviderResultsRepository();
            publishedProviderResultsRepository
                .GetPublishedProviderResultForIdInPublishedState(Arg.Is(allocationResultId))
                .Returns(publishedProviderResult);

            IVersionRepository<PublishedAllocationLineResultVersion> versionRepository = CreatePublishedProviderResultsVersionRepository();
            versionRepository
                .GetVersions(Arg.Is(allocationResultId))
                .Returns(history);

            ResultsService service = CreateResultsService(publishedProviderResultsRepository: publishedProviderResultsRepository, publishedProviderResultsVersionRepository: versionRepository);

            //Act
            PublishedProviderResultWithHistory result = await service.GetPublishedProviderResultWithHistoryByAllocationResultId(allocationResultId);

            //Assert
            result
                .Should()
                .NotBeNull();

            result
                .PublishedProviderResult
                .Should()
                .NotBeNull();

            result
                .History
                .Count()
                .Should()
                .Be(3);
        }
    }
}
