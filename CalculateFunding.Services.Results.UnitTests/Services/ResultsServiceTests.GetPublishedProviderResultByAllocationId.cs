using CalculateFunding.Services.Results.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using NSubstitute;
using CalculateFunding.Models.Results;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;

namespace CalculateFunding.Services.Results.Services
{
    public partial class ResultsServiceTests
    {
        [TestMethod]
        public async Task GetPublishedProviderResultByAllocationResultId_GivenResultNotFound_ResturnsNull()
        {
            //Arrange
            string allocationResultId = "12345";

            IPublishedProviderResultsRepository publishedProviderResultsRepository = CreatePublishedProviderResultsRepository();
            publishedProviderResultsRepository
                .GetPublishedProviderResultForId(Arg.Is(allocationResultId))
                .Returns((PublishedProviderResult)null);

            ResultsService service = CreateResultsService(publishedProviderResultsRepository: publishedProviderResultsRepository);

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
                .GetPublishedProviderResultForId(Arg.Is(allocationResultId))
                .Returns(publishedProviderResult);

            ResultsService service = CreateResultsService(publishedProviderResultsRepository: publishedProviderResultsRepository);

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
                .GetPublishedProviderResultForId(Arg.Is(allocationResultId))
                .Returns(publishedProviderResult);

            ResultsService service = CreateResultsService(publishedProviderResultsRepository: publishedProviderResultsRepository);

            //Act
            PublishedProviderResult result = await service.GetPublishedProviderResultByAllocationResultId(allocationResultId, version);

            //Assert
            result
                .Should()
                .NotBeNull();

            await
                publishedProviderResultsRepository
                    .DidNotReceive()
                    .GetPublishedAllocationLineResultHistoryForId(Arg.Any<string>());
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
                .GetPublishedProviderResultForId(Arg.Is(allocationResultId))
                .Returns(publishedProviderResult);

            publishedProviderResultsRepository
                 .GetPublishedAllocationLineResultHistoryForId(Arg.Is(allocationResultId))
                 .Returns((PublishedAllocationLineResultHistory)null);

            ResultsService service = CreateResultsService(publishedProviderResultsRepository: publishedProviderResultsRepository);

            //Act
            PublishedProviderResult result = await service.GetPublishedProviderResultByAllocationResultId(allocationResultId, version);

            //Assert
            result
                .Should()
                .BeNull();
        }

        [TestMethod]
        public async Task GetPublishedProviderResultByAllocationResultId_GivenVersionButNotFoundInHistory_ReturnsNull()
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

            PublishedAllocationLineResultHistory history = new PublishedAllocationLineResultHistory
            {
                History = new[]
                {
                    new PublishedAllocationLineResultVersion
                    {
                        Version = 5
                    }
                }
            };

            IPublishedProviderResultsRepository publishedProviderResultsRepository = CreatePublishedProviderResultsRepository();
            publishedProviderResultsRepository
                .GetPublishedProviderResultForId(Arg.Is(allocationResultId))
                .Returns(publishedProviderResult);

            publishedProviderResultsRepository
                 .GetPublishedAllocationLineResultHistoryForId(Arg.Is(allocationResultId))
                 .Returns(history);

            ResultsService service = CreateResultsService(publishedProviderResultsRepository: publishedProviderResultsRepository);

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

            PublishedAllocationLineResultHistory history = new PublishedAllocationLineResultHistory
            {
                History = new[]
                {
                    new PublishedAllocationLineResultVersion
                    {
                        Version = 5
                    }
                }
            };

            IPublishedProviderResultsRepository publishedProviderResultsRepository = CreatePublishedProviderResultsRepository();
            publishedProviderResultsRepository
                .GetPublishedProviderResultForId(Arg.Is(allocationResultId))
                .Returns(publishedProviderResult);

            publishedProviderResultsRepository
                 .GetPublishedAllocationLineResultHistoryForId(Arg.Is(allocationResultId))
                 .Returns(history);

            ResultsService service = CreateResultsService(publishedProviderResultsRepository: publishedProviderResultsRepository);

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
