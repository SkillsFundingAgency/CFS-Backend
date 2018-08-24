
using CalculateFunding.Models.Results;
using CalculateFunding.Services.Results.Interfaces;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Text;
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
            PublishedProviderResult result = await service.GetPublishedProviderResultWithHistoryByAllocationResultId(allocationResultId);

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

            publishedProviderResultsRepository
               .GetPublishedAllocationLineResultHistoryForId(Arg.Is(allocationResultId))
               .Returns((PublishedAllocationLineResultHistory)null);

            ResultsService service = CreateResultsService(publishedProviderResultsRepository: publishedProviderResultsRepository);

            //Act
            PublishedProviderResult result = await service.GetPublishedProviderResultWithHistoryByAllocationResultId(allocationResultId);

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

            PublishedAllocationLineResultHistory history = new PublishedAllocationLineResultHistory
            {
                History = new List<PublishedAllocationLineResultVersion>
                {
                    new PublishedAllocationLineResultVersion(),
                    new PublishedAllocationLineResultVersion(),
                    new PublishedAllocationLineResultVersion()
                }
            };

            IPublishedProviderResultsRepository publishedProviderResultsRepository = CreatePublishedProviderResultsRepository();
            publishedProviderResultsRepository
                .GetPublishedProviderResultForIdInPublishedState(Arg.Is(allocationResultId))
                .Returns(publishedProviderResult);

            publishedProviderResultsRepository
               .GetPublishedAllocationLineResultHistoryForId(Arg.Is(allocationResultId))
               .Returns(history);

            ResultsService service = CreateResultsService(publishedProviderResultsRepository: publishedProviderResultsRepository);

            //Act
            PublishedProviderResult result = await service.GetPublishedProviderResultWithHistoryByAllocationResultId(allocationResultId);

            //Assert
            result
                .Should()
                .NotBeNull();

            result
                .FundingStreamResult
                .AllocationLineResult
                .History
                .Count
                .Should()
                .Be(3);
        }
    }
}
