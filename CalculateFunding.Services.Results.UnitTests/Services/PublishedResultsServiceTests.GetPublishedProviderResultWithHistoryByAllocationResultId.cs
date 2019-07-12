using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Models.Results;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Services.Results.Interfaces;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace CalculateFunding.Services.Results.UnitTests.Services
{
    public partial class PublishedResultsServiceTests
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

            PublishedResultsService service = CreateResultsService(publishedProviderResultsRepository: publishedProviderResultsRepository);

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

            string query = $"select c from c where c.documentType = 'PublishedAllocationLineResultVersion' and c.deleted = false and c.content.entityId = '{allocationResultId}'";

            PublishedProviderResult publishedProviderResult = new PublishedProviderResult { ProviderId = "1111" };

            IPublishedProviderResultsRepository publishedProviderResultsRepository = CreatePublishedProviderResultsRepository();
            publishedProviderResultsRepository
                .GetPublishedProviderResultForIdInPublishedState(Arg.Is(allocationResultId))
                .Returns(publishedProviderResult);

            IVersionRepository<PublishedAllocationLineResultVersion> versionRepository = CreatePublishedProviderResultsVersionRepository();
            versionRepository
                .GetVersions(Arg.Is(query), Arg.Is("1111"))
                .Returns((IEnumerable<PublishedAllocationLineResultVersion>)null);

            PublishedResultsService service = CreateResultsService(publishedProviderResultsRepository: publishedProviderResultsRepository, publishedProviderResultsVersionRepository: versionRepository);

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
                ProviderId = "1111",
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
                .GetVersions(Arg.Is(allocationResultId), Arg.Is("1111"))
                .Returns(history);

            PublishedResultsService service = CreateResultsService(publishedProviderResultsRepository: publishedProviderResultsRepository, publishedProviderResultsVersionRepository: versionRepository);

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
