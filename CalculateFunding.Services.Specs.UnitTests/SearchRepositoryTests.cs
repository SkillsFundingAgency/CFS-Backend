using CalculateFunding.Models.Specs;
using CalculateFunding.Repositories.Common.Search;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.Specs
{
    [TestClass]
    public class SearchRepositoryTests
    {
        [TestMethod]
		public void ParseSearchText_GivenNullSearchText_ReturnsEmptyString()
        {
            // Arrange
            string searchText = null;

            // Act
            var result = SearchRepository<SpecificationIndex>.ParseSearchText(searchText);

            // Assert
            result.Should().Be(string.Empty);
        }

        [TestMethod]
        public void ParseSearchText_GivenEmptySearchText_ReturnsEmptyString()
        {
            // Arrange
            string searchText = string.Empty;

            // Act
            var result = SearchRepository<SpecificationIndex>.ParseSearchText(searchText);

            // Assert
            result.Should().Be(string.Empty);
        }

        [TestMethod]
        public void ParseSearchText_GivenSingleTermSearchText_ReturnsWildcardSearchText()
        {
            // Arrange
            string searchText = "simple";

            // Act
            var result = SearchRepository<SpecificationIndex>.ParseSearchText(searchText);

            // Assert
            result.Should().Be("simple*");
        }

        [TestMethod]
        public void ParseSearchText_GivenMultiTermSearchText_ReturnsWildcardSearchText()
        {
            // Arrange
            string searchText = "two terms";

            // Act
            var result = SearchRepository<SpecificationIndex>.ParseSearchText(searchText);

            // Assert
            result.Should().Be("two* terms*");
        }

        [TestMethod]
        public void ParseSearchText_GivenQuotedSearchText_ReturnsSimpleWildcardSearchText()
        {
            // Arrange
            string searchText = "\"quoted\"";

            // Act
            var result = SearchRepository<SpecificationIndex>.ParseSearchText(searchText);

            // Assert
            result.Should().Be("quoted*");
        }

        [TestMethod]
        public void ParseSearchText_GivenMultipleTermSearchTextWithMultipleSpacesBetween_ReturnsWildcardSearchText()
        {
            // Arrange
            string searchText = "too many   spaces";

            // Act
            var result = SearchRepository<SpecificationIndex>.ParseSearchText(searchText);

            // Assert
            result.Should().Be("too* many* spaces*");
        }

        [TestMethod]
        public void ParseSearchText_GivenSimpleSearchTextWithSpaceAtEnd_ReturnsWildcardSearchText()
        {
            // Arrange
            string searchText = "simple ";

            // Act
            var result = SearchRepository<SpecificationIndex>.ParseSearchText(searchText);

            // Assert
            result.Should().Be("simple*");
        }
    }
}
