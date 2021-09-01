using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Models.External;
using CalculateFunding.Models.External.AtomItems;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Models.UnitTests
{
    [TestClass]
    public class SearchFeedV3Tests
    {
        [TestMethod]
        public void Getters_WhenOnAMiddlePage_ShouldReturnCorrectGeneratedValues()
        {
            // arrange
            SearchFeedResult<string> SearchFeedV3 = new SearchFeedResult<string>
            {
                Top = 10,
                TotalCount = 50,
                PageRef = 3
            };

            // assert & act
            SearchFeedV3
                .Current
                .Should().Be(3);

            SearchFeedV3
                .PreviousArchive
                .Should().Be(2);

            SearchFeedV3
                .NextArchive
                .Should().Be(4);

            SearchFeedV3
                .TotalPages
                .Should().Be(5);

            SearchFeedV3
                .IsArchivePage
                .Should().BeTrue();
        }

        [TestMethod]
        public void Getters_WhenOnPenultimatePage_ShouldReturnCorrectGeneratedValues()
        {
            // arrange
            SearchFeedResult<string> SearchFeedV3 = new SearchFeedResult<string>
            {
                Top = 10,
                TotalCount = 45,
                PageRef = 4
            };

            // assert & act
            SearchFeedV3
                .Current
                .Should().Be(4);

            SearchFeedV3
                .PreviousArchive
                .Should().Be(3);

            SearchFeedV3
                .NextArchive
                .Should().BeNull();

            SearchFeedV3
                .Current
                .Should().Be(4);

            SearchFeedV3
                .TotalPages
                .Should().Be(5);

            SearchFeedV3
                .IsArchivePage
                .Should().BeTrue();
        }

        [TestMethod]
        public void Getters_WhenOnSubscriptionPage_ShouldReturnCorrectGeneratedValues()
        {
            // arrange
            SearchFeedResult<string> SearchFeedV3 = new SearchFeedResult<string>
            {
                Top = 10,
                TotalCount = 45,
                PageRef = 5
            };

            // assert & act
            SearchFeedV3
                .Current
                .Should().BeNull();

            SearchFeedV3
                .PreviousArchive
                .Should().Be(4);

            SearchFeedV3
                .NextArchive
                .Should().BeNull();

            SearchFeedV3
                .Current
                .Should().BeNull();

            SearchFeedV3
                .TotalPages
                .Should().Be(5);

            SearchFeedV3
                .IsArchivePage
                .Should().BeFalse();
        }

        [TestMethod]
        public void Getters_WhenOnFirstPage_ShouldReturnCorrectGeneratedValues()
        {
            // arrange
            SearchFeedResult<string> SearchFeedV3 = new SearchFeedResult<string>
            {
                Top = 10,
                TotalCount = 45,
                PageRef = 1
            };

            // assert & act
            SearchFeedV3
                .Current
                .Should().Be(1);

            SearchFeedV3
                .PreviousArchive
                .Should().BeNull();

            SearchFeedV3
                .NextArchive
                .Should().Be(2);

            SearchFeedV3
                .TotalPages
                .Should().Be(5);

            SearchFeedV3
                .IsArchivePage
                .Should().BeTrue();
        }

        [TestMethod]
        public void GenerateAtomLinksForResultGivenBaseUrl_WhenOnAMiddlePage_ShouldReturnCorrectGeneratedValues()
        {
            // arrange
            SearchFeedResult<string> SearchFeedV3 = new SearchFeedResult<string>
            {
                Top = 10,
                TotalCount = 50,
                PageRef = 3
            };

            // act
            IList<AtomLink> atomLinksForFeed = SearchFeedV3.GenerateAtomLinksForResultGivenBaseUrl("https://localhost:5009/api/v2/allocations/notifications{0}");

            // assert
            atomLinksForFeed
                .Count
                .Should().Be(4);

            var atomLink1 = atomLinksForFeed.First();
            atomLink1
                .Href
                .Should().Be("https://localhost:5009/api/v2/allocations/notifications/2");
            atomLink1
                .Rel
                .Should().Be("prev-archive");

            var atomLink2 = atomLinksForFeed.ElementAt(1);
            atomLink2
                .Href
                .Should().Be("https://localhost:5009/api/v2/allocations/notifications/4");
            atomLink2
                .Rel
                .Should().Be("next-archive");

            var atomLink3 = atomLinksForFeed.ElementAt(2);
            atomLink3
                .Href
                .Should().Be("https://localhost:5009/api/v2/allocations/notifications/3");
            atomLink3
                .Rel
                .Should().Be("current");

            var atomLink4 = atomLinksForFeed.ElementAt(3);
            atomLink4
                .Href
                .Should().Be("https://localhost:5009/api/v2/allocations/notifications");
            atomLink4
                .Rel
                .Should().Be("self");
        }
    }
}
