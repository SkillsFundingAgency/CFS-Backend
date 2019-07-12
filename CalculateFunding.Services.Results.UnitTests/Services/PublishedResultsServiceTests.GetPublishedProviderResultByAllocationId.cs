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
        public async Task GetPublishedProviderResultByAllocationResultId_GivenResultNotFound_ResturnsNull()
        {
            //Arrange
            string allocationResultId = "12345";

            IPublishedProviderResultsRepository publishedProviderResultsRepository = CreatePublishedProviderResultsRepository();
            publishedProviderResultsRepository
                .GetPublishedProviderResultForIdInPublishedState(Arg.Is(allocationResultId))
                .Returns((PublishedProviderResult)null);

            PublishedResultsService service = CreateResultsService(publishedProviderResultsRepository: publishedProviderResultsRepository);

            //Act
            PublishedProviderResult result = await service.GetPublishedProviderResultByAllocationResultId(allocationResultId);

            //Assert
            result
                .Should()
                .BeNull();
        }

        [TestMethod]
        public async Task GetPublishedProviderResultByAllocationResultId_GivenResultFound_ResturnsResult()
        {
            //Arrange
            string allocationResultId = "12345";

            PublishedProviderResult publishedProviderResult = new PublishedProviderResult();

            IPublishedProviderResultsRepository publishedProviderResultsRepository = CreatePublishedProviderResultsRepository();
            publishedProviderResultsRepository
                .GetPublishedProviderResultForIdInPublishedState(Arg.Is(allocationResultId))
                .Returns(publishedProviderResult);

            PublishedResultsService service = CreateResultsService(publishedProviderResultsRepository: publishedProviderResultsRepository);

            //Act
            PublishedProviderResult result = await service.GetPublishedProviderResultByAllocationResultId(allocationResultId);

            //Assert
            result
                .Should()
                .NotBeNull();
        }

        [TestMethod]
        public async Task GetPublishedProviderResultByAllocationResultId_GivenVersionSuppliedButAlreadyCurrent_ReturnsResultDoesNotFetchHistory()
        {
            //Arrange
            string allocationResultId = "12345";

            int version = 1;

            PublishedProviderResult publishedProviderResult = new PublishedProviderResult
            {
                FundingStreamResult = new PublishedFundingStreamResult
                {
                    AllocationLineResult = new PublishedAllocationLineResult
                    {
                        Current = new PublishedAllocationLineResultVersion { Version = version }
                    }
                }
            };

            IPublishedProviderResultsRepository publishedProviderResultsRepository = CreatePublishedProviderResultsRepository();
            publishedProviderResultsRepository
                .GetPublishedProviderResultForIdInPublishedState(Arg.Is(allocationResultId))
                .Returns(publishedProviderResult);

            IVersionRepository<PublishedAllocationLineResultVersion> versionRepository = CreatePublishedProviderResultsVersionRepository();

            PublishedResultsService service = CreateResultsService(publishedProviderResultsRepository: publishedProviderResultsRepository, publishedProviderResultsVersionRepository: versionRepository);

            //Act
            PublishedProviderResult result = await service.GetPublishedProviderResultByAllocationResultId(allocationResultId, version);

            //Assert
            result
                .Should()
                .NotBeNull();

            await
                versionRepository
                    .DidNotReceive()
                    .GetVersion(Arg.Any<string>(), Arg.Any<int>());
        }

        [TestMethod]
        public async Task GetPublishedProviderResultByAllocationResultId_GivenVersionButHistoryNotReturned_ReturnsNull()
        {
            //Arrange
            string allocationResultId = "12345";

            int version = 1;

            PublishedProviderResult publishedProviderResult = new PublishedProviderResult
            {
                FundingStreamResult = new PublishedFundingStreamResult
                {
                    AllocationLineResult = new PublishedAllocationLineResult
                    {
                        Current = new PublishedAllocationLineResultVersion { Version = 2 }
                    }
                }
            };

            IPublishedProviderResultsRepository publishedProviderResultsRepository = CreatePublishedProviderResultsRepository();
            publishedProviderResultsRepository
                .GetPublishedProviderResultForIdInPublishedState(Arg.Is(allocationResultId))
                .Returns(publishedProviderResult);

            IVersionRepository<PublishedAllocationLineResultVersion> versionRepository = CreatePublishedProviderResultsVersionRepository();
            versionRepository
                .GetVersion(Arg.Is(allocationResultId), Arg.Is(version))
                .Returns((PublishedAllocationLineResultVersion)null);

            PublishedResultsService service = CreateResultsService(publishedProviderResultsRepository: publishedProviderResultsRepository, publishedProviderResultsVersionRepository: versionRepository);

            //Act
            PublishedProviderResult result = await service.GetPublishedProviderResultByAllocationResultId(allocationResultId, version);

            //Assert
            result
                .Should()
                .BeNull();
        }


        [TestMethod]
        public async Task GetPublishedProviderResultByAllocationResultId_GivenVersionAndFoundInHistory_ReturnsResult()
        {
            //Arrange
            string allocationResultId = "12345";

            int version = 5;

            PublishedProviderResult publishedProviderResult = new PublishedProviderResult
            {
                FundingStreamResult = new PublishedFundingStreamResult
                {
                    AllocationLineResult = new PublishedAllocationLineResult
                    {
                        Current = new PublishedAllocationLineResultVersion { Version = 2 }
                    }
                }
            };

            PublishedAllocationLineResultVersion publishedAllocationLineResultVersion = new PublishedAllocationLineResultVersion
            {
                Version = 5
            };

            IPublishedProviderResultsRepository publishedProviderResultsRepository = CreatePublishedProviderResultsRepository();
            publishedProviderResultsRepository
                .GetPublishedProviderResultForIdInPublishedState(Arg.Is(allocationResultId))
                .Returns(publishedProviderResult);

            IVersionRepository<PublishedAllocationLineResultVersion> versionRepository = CreatePublishedProviderResultsVersionRepository();
            versionRepository
                .GetVersion(Arg.Is(allocationResultId), Arg.Is(version))
                .Returns(publishedAllocationLineResultVersion);

            PublishedResultsService service = CreateResultsService(publishedProviderResultsRepository: publishedProviderResultsRepository, publishedProviderResultsVersionRepository: versionRepository);

            //Act
            PublishedProviderResult result = await service.GetPublishedProviderResultByAllocationResultId(allocationResultId, version);

            //Assert
            result
                .Should()
                .NotBeNull();

            result
               .FundingStreamResult
               .AllocationLineResult
               .Current
               .Version
               .Should()
               .Be(5);
        }
    }
}
