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
    public partial class PublishedResultsServiceTests
    {
        [TestMethod]
        public void GetPublishedProviderResultByVersionId_GivenVersionCannotBeFound_ReturnsNull()
        {
            //Arrange
            string id = "id-1";

            IPublishedProviderResultsRepository publishedProviderResultsRepository = CreatePublishedProviderResultsRepository();
            publishedProviderResultsRepository
                .GetPublishedProviderResultVersionForFeedIndexId(Arg.Is(id))
                .Returns((PublishedAllocationLineResultVersion)null);

            PublishedResultsService publishedResultsService = CreateResultsService(publishedProviderResultsRepository: publishedProviderResultsRepository);

            //Act
            PublishedProviderResult result = publishedResultsService.GetPublishedProviderResultByVersionId(id);

            //Assert
            result
                .Should()
                .BeNull();
        }

        [TestMethod]
        public void GetPublishedProviderResultByVersionId_GivenVersionFoundButResultCanbnotBeFound_ReturnsNull()
        {
            //Arrange
            string id = "id-1";
            string entityId = "entity-id-1";

            PublishedAllocationLineResultVersion version = new PublishedAllocationLineResultVersion
            {
                PublishedProviderResultId = entityId,
                FeedIndexId = id
            };

            IPublishedProviderResultsRepository publishedProviderResultsRepository = CreatePublishedProviderResultsRepository();
            publishedProviderResultsRepository
                .GetPublishedProviderResultVersionForFeedIndexId(Arg.Is(id))
                .Returns(version);

            publishedProviderResultsRepository
                .GetPublishedProviderResultForId(Arg.Is(entityId))
                .Returns((PublishedProviderResult)null);

            PublishedResultsService publishedResultsService = CreateResultsService(publishedProviderResultsRepository: publishedProviderResultsRepository);

            //Act
            PublishedProviderResult result = publishedResultsService.GetPublishedProviderResultByVersionId(id);

            //Assert
            result
                .Should()
                .BeNull();
        }

        [TestMethod]
        public void GetPublishedProviderResultByVersionId_GivenResultFound_ReturnsResult()
        {
            //Arrange
            string id = "id-1";
            string entityId = "entity-id-1";

            PublishedAllocationLineResultVersion version = new PublishedAllocationLineResultVersion
            {
                PublishedProviderResultId = entityId,
                FeedIndexId = id
            };

            PublishedProviderResult publishedProviderResult = new PublishedProviderResult
            {
                FundingStreamResult = new PublishedFundingStreamResult
                {
                    AllocationLineResult = new PublishedAllocationLineResult()
                }
            };

            IPublishedProviderResultsRepository publishedProviderResultsRepository = CreatePublishedProviderResultsRepository();
            publishedProviderResultsRepository
                .GetPublishedProviderResultVersionForFeedIndexId(Arg.Is(id))
                .Returns(version);

            publishedProviderResultsRepository
                .GetPublishedProviderResultForId(Arg.Is(entityId))
                .Returns(publishedProviderResult);

            PublishedResultsService publishedResultsService = CreateResultsService(publishedProviderResultsRepository: publishedProviderResultsRepository);

            //Act
            PublishedProviderResult result = publishedResultsService.GetPublishedProviderResultByVersionId(id);

            //Assert
            result
                .Should()
                .NotBeNull();

            result
                .FundingStreamResult
                .AllocationLineResult
                .Current
                .Should()
                .Be(version);
        }
    }
}
