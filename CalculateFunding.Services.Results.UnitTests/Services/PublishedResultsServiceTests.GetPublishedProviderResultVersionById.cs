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
        public void GetPublishedProviderResultVersionById_GivenVersionCannotBeFound_ReturnsNull()
        {
            //Arrange
            string feedIndexId = "id-1";

            IPublishedProviderResultsRepository publishedProviderResultsRepository = CreatePublishedProviderResultsRepository();
            publishedProviderResultsRepository
                .GetPublishedProviderResultVersionForFeedIndexId(Arg.Is(feedIndexId))
                .Returns((PublishedAllocationLineResultVersion)null);

            PublishedResultsService publishedResultsService = CreateResultsService(publishedProviderResultsRepository: publishedProviderResultsRepository);

            //Act
            PublishedAllocationLineResultVersion result = publishedResultsService.GetPublishedProviderResultVersionById(feedIndexId);

            //Assert
            result
                .Should()
                .BeNull();
        }

        [TestMethod]
        public void GetPublishedProviderResultVersionById_GivenVersionWasFound_ReturnsVersion()
        {
            //Arrange
            string feedIndexId = "id-1";

            PublishedAllocationLineResultVersion publishedAllocationLineResultVersion = new PublishedAllocationLineResultVersion();

            IPublishedProviderResultsRepository publishedProviderResultsRepository = CreatePublishedProviderResultsRepository();
            publishedProviderResultsRepository
                .GetPublishedProviderResultVersionForFeedIndexId(Arg.Is(feedIndexId))
                .Returns(publishedAllocationLineResultVersion);

            PublishedResultsService publishedResultsService = CreateResultsService(publishedProviderResultsRepository: publishedProviderResultsRepository);

            //Act
            PublishedAllocationLineResultVersion result = publishedResultsService.GetPublishedProviderResultVersionById(feedIndexId);

            //Assert
            result
                .Should()
                .NotBeNull();

            result
                .Should()
                .Be(publishedAllocationLineResultVersion);
        }
    }
}
