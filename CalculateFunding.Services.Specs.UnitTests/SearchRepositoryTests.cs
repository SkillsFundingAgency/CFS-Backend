using CalculateFunding.Models.Specs;
using CalculateFunding.Repositories.Common.Search;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.Specs.UnitTests
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

        [TestMethod]
        public void ParseSearchText_GivenSimpleSearchTextPlusSpecialCharacter_ReturnsEscapedSearchText()
        {
            // Arrange
            string searchText = "+";

            // Act
            var result = SearchRepository<SpecificationIndex>.ParseSearchText(searchText);

            // Assert
            result.Should().Be("\\+*");
        }

        [TestMethod]
        public void ParseSearchText_GivenSimpleSearchTextMinusSpecialCharacter_ReturnsEscapedSearchText()
        {
            // Arrange
            string searchText = "-";

            // Act
            var result = SearchRepository<SpecificationIndex>.ParseSearchText(searchText);

            // Assert
            result.Should().Be("\\-*");
        }

        [TestMethod]
        public void ParseSearchText_GivenSimpleSearchTextAmpersandSpecialCharacter_ReturnsEscapedSearchText()
        {
            // Arrange
            string searchText = "&&";

            // Act
            var result = SearchRepository<SpecificationIndex>.ParseSearchText(searchText);

            // Assert
            result.Should().Be("\\&&*");
        }

        [TestMethod]
        public void ParseSearchText_GivenSimpleSearchTextOrSpecialCharacter_ReturnsEscapedSearchText()
        {
            // Arrange
            string searchText = "||";

            // Act
            var result = SearchRepository<SpecificationIndex>.ParseSearchText(searchText);

            // Assert
            result.Should().Be("\\||*");
        }

        [TestMethod]
        public void ParseSearchText_GivenSimpleSearchTextExclamationSpecialCharacter_ReturnsEscapedSearchText()
        {
            // Arrange
            string searchText = "!";

            // Act
            var result = SearchRepository<SpecificationIndex>.ParseSearchText(searchText);

            // Assert
            result.Should().Be("\\!*");
        }

        [TestMethod]
        public void ParseSearchText_GivenSimpleSearchTextLeftBracketSpecialCharacter_ReturnsEscapedSearchText()
        {
            // Arrange
            string searchText = "(";

            // Act
            var result = SearchRepository<SpecificationIndex>.ParseSearchText(searchText);

            // Assert
            result.Should().Be("\\(*");
        }

        [TestMethod]
        public void ParseSearchText_GivenSimpleSearchTextRightBracketSpecialCharacter_ReturnsEscapedSearchText()
        {
            // Arrange
            string searchText = ")";

            // Act
            var result = SearchRepository<SpecificationIndex>.ParseSearchText(searchText);

            // Assert
            result.Should().Be("\\)*");
        }

        [TestMethod]
        public void ParseSearchText_GivenSimpleSearchTextLeftCurlySpecialCharacter_ReturnsEscapedSearchText()
        {
            // Arrange
            string searchText = "{";

            // Act
            var result = SearchRepository<SpecificationIndex>.ParseSearchText(searchText);

            // Assert
            result.Should().Be("\\{*");
        }

        [TestMethod]
        public void ParseSearchText_GivenSimpleSearchTextRightCurlySpecialCharacter_ReturnsEscapedSearchText()
        {
            // Arrange
            string searchText = "}";

            // Act
            var result = SearchRepository<SpecificationIndex>.ParseSearchText(searchText);

            // Assert
            result.Should().Be("\\}*");
        }

        [TestMethod]
        public void ParseSearchText_GivenSimpleSearchTextLeftSquareSpecialCharacter_ReturnsEscapedSearchText()
        {
            // Arrange
            string searchText = "[";

            // Act
            var result = SearchRepository<SpecificationIndex>.ParseSearchText(searchText);

            // Assert
            result.Should().Be("\\[*");
        }

        [TestMethod]
        public void ParseSearchText_GivenSimpleSearchTextRightSquareSpecialCharacter_ReturnsEscapedSearchText()
        {
            // Arrange
            string searchText = "]";

            // Act
            var result = SearchRepository<SpecificationIndex>.ParseSearchText(searchText);

            // Assert
            result.Should().Be("\\]*");
        }

        [TestMethod]
        public void ParseSearchText_GivenSimpleSearchTextCaretSpecialCharacter_ReturnsEscapedSearchText()
        {
            // Arrange
            string searchText = "^";

            // Act
            var result = SearchRepository<SpecificationIndex>.ParseSearchText(searchText);

            // Assert
            result.Should().Be("\\^*");
        }

        [TestMethod]
        public void ParseSearchText_GivenSimpleSearchTextTildeSpecialCharacter_ReturnsEscapedSearchText()
        {
            // Arrange
            string searchText = "~";

            // Act
            var result = SearchRepository<SpecificationIndex>.ParseSearchText(searchText);

            // Assert
            result.Should().Be("\\~*");
        }

        [TestMethod]
        public void ParseSearchText_GivenSimpleSearchTextAsterixSpecialCharacter_ReturnsEscapedSearchText()
        {
            // Arrange
            string searchText = "*";

            // Act
            var result = SearchRepository<SpecificationIndex>.ParseSearchText(searchText);

            // Assert
            result.Should().Be("\\**");
        }

        [TestMethod]
        public void ParseSearchText_GivenSimpleSearchTextQuestionMarkSpecialCharacter_ReturnsEscapedSearchText()
        {
            // Arrange
            string searchText = "?";

            // Act
            var result = SearchRepository<SpecificationIndex>.ParseSearchText(searchText);

            // Assert
            result.Should().Be("\\?*");
        }

        [TestMethod]
        public void ParseSearchText_GivenSimpleSearchTextColonSpecialCharacter_ReturnsEscapedSearchText()
        {
            // Arrange
            string searchText = ":";

            // Act
            var result = SearchRepository<SpecificationIndex>.ParseSearchText(searchText);

            // Assert
            result.Should().Be("\\:*");
        }

        [TestMethod]
        public void ParseSearchText_GivenSimpleSearchTextBackslashSpecialCharacter_ReturnsEscapedSearchText()
        {
            // Arrange
            string searchText = "\\";

            // Act
            var result = SearchRepository<SpecificationIndex>.ParseSearchText(searchText);

            // Assert
            result.Should().Be("\\\\*");
        }

        [TestMethod]
        public void ParseSearchText_GivenSimpleSearchTextForwardSlashSpecialCharacter_ReturnsEscapedSearchText()
        {
            // Arrange
            string searchText = "/";

            // Act
            var result = SearchRepository<SpecificationIndex>.ParseSearchText(searchText);

            // Assert
            result.Should().Be("\\/*");
        }
    }
}
