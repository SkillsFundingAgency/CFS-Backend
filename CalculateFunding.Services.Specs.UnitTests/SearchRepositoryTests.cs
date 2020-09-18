using System;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Models.Specs;
using CalculateFunding.Repositories.Common.Search;
using FluentAssertions;
using Microsoft.Azure.Search;
using Microsoft.Azure.Search.Models;
using Microsoft.Rest.Azure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

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
            string result = SearchRepository<SpecificationIndex>.ParseSearchText(searchText);

            // Assert
            result.Should().Be(string.Empty);
        }

        [TestMethod]
        public void ParseSearchText_GivenEmptySearchText_ReturnsEmptyString()
        {
            // Arrange
            string searchText = string.Empty;

            // Act
            string result = SearchRepository<SpecificationIndex>.ParseSearchText(searchText);

            // Assert
            result.Should().Be(string.Empty);
        }

        [TestMethod]
        public void ParseSearchText_GivenSingleTermSearchText_ReturnsWildcardSearchText()
        {
            // Arrange
            string searchText = "simple";

            // Act
            string result = SearchRepository<SpecificationIndex>.ParseSearchText(searchText);

            // Assert
            result.Should().Be("simple*");
        }

        [TestMethod]
        public void ParseSearchText_GivenMultiTermSearchText_ReturnsWildcardSearchText()
        {
            // Arrange
            string searchText = "two terms";

            // Act
            string result = SearchRepository<SpecificationIndex>.ParseSearchText(searchText);

            // Assert
            result.Should().Be("two* terms*");
        }

        [TestMethod]
        public void ParseSearchText_GivenQuotedSearchText_ReturnsSimpleWildcardSearchText()
        {
            // Arrange
            string searchText = "\"quoted\"";

            // Act
            string result = SearchRepository<SpecificationIndex>.ParseSearchText(searchText);

            // Assert
            result.Should().Be("quoted*");
        }

        [TestMethod]
        public void ParseSearchText_GivenMultipleTermSearchTextWithMultipleSpacesBetween_ReturnsWildcardSearchText()
        {
            // Arrange
            string searchText = "too many   spaces";

            // Act
            string result = SearchRepository<SpecificationIndex>.ParseSearchText(searchText);

            // Assert
            result.Should().Be("too* many* spaces*");
        }

        [TestMethod]
        public void ParseSearchText_GivenSimpleSearchTextWithSpaceAtEnd_ReturnsWildcardSearchText()
        {
            // Arrange
            string searchText = "simple ";

            // Act
            string result = SearchRepository<SpecificationIndex>.ParseSearchText(searchText);

            // Assert
            result.Should().Be("simple*");
        }

        [TestMethod]
        public void ParseSearchText_GivenSimpleSearchTextPlusSpecialCharacter_ReturnsEscapedSearchText()
        {
            // Arrange
            string searchText = "+";

            // Act
            string result = SearchRepository<SpecificationIndex>.ParseSearchText(searchText);

            // Assert
            result.Should().Be("\\+*");
        }

        [TestMethod]
        public void ParseSearchText_GivenSimpleSearchTextMinusSpecialCharacter_ReturnsEscapedSearchText()
        {
            // Arrange
            string searchText = "-";

            // Act
            string result = SearchRepository<SpecificationIndex>.ParseSearchText(searchText);

            // Assert
            result.Should().Be("\\-*");
        }

        [TestMethod]
        public void ParseSearchText_GivenSimpleSearchTextAmpersandSpecialCharacter_ReturnsEscapedSearchText()
        {
            // Arrange
            string searchText = "&&";

            // Act
            string result = SearchRepository<SpecificationIndex>.ParseSearchText(searchText);

            // Assert
            result.Should().Be("\\&&*");
        }

        [TestMethod]
        public void ParseSearchText_GivenSimpleSearchTextOrSpecialCharacter_ReturnsEscapedSearchText()
        {
            // Arrange
            string searchText = "||";

            // Act
            string result = SearchRepository<SpecificationIndex>.ParseSearchText(searchText);

            // Assert
            result.Should().Be("\\||*");
        }

        [TestMethod]
        public void ParseSearchText_GivenSimpleSearchTextExclamationSpecialCharacter_ReturnsEscapedSearchText()
        {
            // Arrange
            string searchText = "!";

            // Act
            string result = SearchRepository<SpecificationIndex>.ParseSearchText(searchText);

            // Assert
            result.Should().Be("\\!*");
        }

        [TestMethod]
        public void ParseSearchText_GivenSimpleSearchTextLeftBracketSpecialCharacter_ReturnsEscapedSearchText()
        {
            // Arrange
            string searchText = "(";

            // Act
            string result = SearchRepository<SpecificationIndex>.ParseSearchText(searchText);

            // Assert
            result.Should().Be("\\(*");
        }

        [TestMethod]
        public void ParseSearchText_GivenSimpleSearchTextRightBracketSpecialCharacter_ReturnsEscapedSearchText()
        {
            // Arrange
            string searchText = ")";

            // Act
            string result = SearchRepository<SpecificationIndex>.ParseSearchText(searchText);

            // Assert
            result.Should().Be("\\)*");
        }

        [TestMethod]
        public void ParseSearchText_GivenSimpleSearchTextLeftCurlySpecialCharacter_ReturnsEscapedSearchText()
        {
            // Arrange
            string searchText = "{";

            // Act
            string result = SearchRepository<SpecificationIndex>.ParseSearchText(searchText);

            // Assert
            result.Should().Be("\\{*");
        }

        [TestMethod]
        public void ParseSearchText_GivenSimpleSearchTextRightCurlySpecialCharacter_ReturnsEscapedSearchText()
        {
            // Arrange
            string searchText = "}";

            // Act
            string result = SearchRepository<SpecificationIndex>.ParseSearchText(searchText);

            // Assert
            result.Should().Be("\\}*");
        }

        [TestMethod]
        public void ParseSearchText_GivenSimpleSearchTextLeftSquareSpecialCharacter_ReturnsEscapedSearchText()
        {
            // Arrange
            string searchText = "[";

            // Act
            string result = SearchRepository<SpecificationIndex>.ParseSearchText(searchText);

            // Assert
            result.Should().Be("\\[*");
        }

        [TestMethod]
        public void ParseSearchText_GivenSimpleSearchTextRightSquareSpecialCharacter_ReturnsEscapedSearchText()
        {
            // Arrange
            string searchText = "]";

            // Act
            string result = SearchRepository<SpecificationIndex>.ParseSearchText(searchText);

            // Assert
            result.Should().Be("\\]*");
        }

        [TestMethod]
        public void ParseSearchText_GivenSimpleSearchTextCaretSpecialCharacter_ReturnsEscapedSearchText()
        {
            // Arrange
            string searchText = "^";

            // Act
            string result = SearchRepository<SpecificationIndex>.ParseSearchText(searchText);

            // Assert
            result.Should().Be("\\^*");
        }

        [TestMethod]
        public void ParseSearchText_GivenSimpleSearchTextTildeSpecialCharacter_ReturnsEscapedSearchText()
        {
            // Arrange
            string searchText = "~";

            // Act
            string result = SearchRepository<SpecificationIndex>.ParseSearchText(searchText);

            // Assert
            result.Should().Be("\\~*");
        }

        [TestMethod]
        public void ParseSearchText_GivenSimpleSearchTextAsterixSpecialCharacter_ReturnsEscapedSearchText()
        {
            // Arrange
            string searchText = "*";

            // Act
            string result = SearchRepository<SpecificationIndex>.ParseSearchText(searchText);

            // Assert
            result.Should().Be("\\**");
        }

        [TestMethod]
        public void ParseSearchText_GivenSimpleSearchTextQuestionMarkSpecialCharacter_ReturnsEscapedSearchText()
        {
            // Arrange
            string searchText = "?";

            // Act
            string result = SearchRepository<SpecificationIndex>.ParseSearchText(searchText);

            // Assert
            result.Should().Be("\\?*");
        }

        [TestMethod]
        public void ParseSearchText_GivenSimpleSearchTextColonSpecialCharacter_ReturnsEscapedSearchText()
        {
            // Arrange
            string searchText = ":";

            // Act
            string result = SearchRepository<SpecificationIndex>.ParseSearchText(searchText);

            // Assert
            result.Should().Be("\\:*");
        }

        [TestMethod]
        public void ParseSearchText_GivenSimpleSearchTextBackslashSpecialCharacter_ReturnsEscapedSearchText()
        {
            // Arrange
            string searchText = "\\";

            // Act
            string result = SearchRepository<SpecificationIndex>.ParseSearchText(searchText);

            // Assert
            result.Should().Be("\\\\*");
        }

        [TestMethod]
        public void ParseSearchText_GivenSimpleSearchTextForwardSlashSpecialCharacter_ReturnsEscapedSearchText()
        {
            // Arrange
            string searchText = "/";

            // Act
            string result = SearchRepository<SpecificationIndex>.ParseSearchText(searchText);

            // Assert
            result.Should().Be("\\/*");
        }

        [TestMethod]
        public async Task SearchById_GivenIdDoesNotReturnSearchResult_ReturnsNull()
        {
            SearchRepositorySettings searchRepositorySettings = new SearchRepositorySettings
            {
                SearchKey = string.Empty,
                SearchServiceName = string.Empty
            };

            ISearchInitializer searchInitializer = Substitute.For<ISearchInitializer>();

            ISearchIndexClient searchIndexClient = Substitute.For<ISearchIndexClient>();

            AzureOperationResponse<DocumentSearchResult<SpecificationIndex>> documentSearchResult =
                new AzureOperationResponse<DocumentSearchResult<SpecificationIndex>>
                {
                    Body = new DocumentSearchResult<SpecificationIndex>(null, null, null, null, null)
                };

            IDocumentsOperations documentsOperations = Substitute.For<IDocumentsOperations>();
            documentsOperations.SearchWithHttpMessagesAsync<SpecificationIndex>(Arg.Any<string>(), Arg.Any<SearchParameters>()).Returns(Task.FromResult(documentSearchResult));

            ISearchServiceClient searchServiceClient = Substitute.For<ISearchServiceClient>();
            searchIndexClient.Documents.Returns(documentsOperations);

            SearchRepository<SpecificationIndex> searchRepository = new SearchRepository<SpecificationIndex>(searchRepositorySettings, searchInitializer, searchServiceClient, searchIndexClient);

            string notFoundId = "notFound";

            SpecificationIndex specificationIndex = await searchRepository.SearchById(notFoundId);

            specificationIndex.Should().BeNull();
        }

        [TestMethod]
        public async Task SearchById_GivenIdReturnsSearchResult_ReturnsResults()
        {
            string existingId = "existingId";

            SearchRepositorySettings searchRepositorySettings = new SearchRepositorySettings
            {
                SearchKey = string.Empty,
                SearchServiceName = string.Empty
            };

            ISearchInitializer searchInitializer = Substitute.For<ISearchInitializer>();

            ISearchIndexClient searchIndexClient = Substitute.For<ISearchIndexClient>();

            Microsoft.Azure.Search.Models.SearchResult<SpecificationIndex> specificationIndexSearchResult = new Microsoft.Azure.Search.Models.SearchResult<SpecificationIndex>(new SpecificationIndex
            {
                Id = existingId
            });

            AzureOperationResponse<DocumentSearchResult<SpecificationIndex>> documentSearchResult =
                new AzureOperationResponse<DocumentSearchResult<SpecificationIndex>>
                {
                    Body = new DocumentSearchResult<SpecificationIndex>(new[]
                        {
                            specificationIndexSearchResult
                        },
                        null,
                        null,
                        null,
                        null)
                };

            IDocumentsOperations documentsOperations = Substitute.For<IDocumentsOperations>();
            documentsOperations
                .SearchWithHttpMessagesAsync<SpecificationIndex>(Arg.Is<string>(_ => _ == $"\"{existingId}\""),
                    Arg.Is<SearchParameters>(_ => _.SearchFields.SequenceEqual(new[]
                    {
                        "id"
                    })))
                .Returns(Task.FromResult(documentSearchResult));

            ISearchServiceClient searchServiceClient = Substitute.For<ISearchServiceClient>();
            searchIndexClient.Documents.Returns(documentsOperations);

            SearchRepository<SpecificationIndex> searchRepository = new SearchRepository<SpecificationIndex>(searchRepositorySettings, searchInitializer, searchServiceClient, searchIndexClient);

            SpecificationIndex specificationIndex = await searchRepository.SearchById(existingId);

            specificationIndex.Should().NotBeNull();
            specificationIndex.Id.Should().Be(existingId);
        }

        [TestMethod]
        public void SearchById_GivenIdReturnsSearchResult_TreatsMultipleResultsAsAnException()
        {
            string existingId = "existingId";

            SearchRepositorySettings searchRepositorySettings = new SearchRepositorySettings
            {
                SearchKey = string.Empty,
                SearchServiceName = string.Empty
            };

            ISearchInitializer searchInitializer = Substitute.For<ISearchInitializer>();

            ISearchIndexClient searchIndexClient = Substitute.For<ISearchIndexClient>();

            Microsoft.Azure.Search.Models.SearchResult<SpecificationIndex> specificationIndexSearchResult = new Microsoft.Azure.Search.Models.SearchResult<SpecificationIndex>(new SpecificationIndex
            {
                Id = existingId
            });

            AzureOperationResponse<DocumentSearchResult<SpecificationIndex>> documentSearchResult =
                new AzureOperationResponse<DocumentSearchResult<SpecificationIndex>>
                {
                    Body = new DocumentSearchResult<SpecificationIndex>(new[]
                        {
                            specificationIndexSearchResult, new Microsoft.Azure.Search.Models.SearchResult<SpecificationIndex>()
                        },
                        null,
                        null,
                        null,
                        null)
                };

            IDocumentsOperations documentsOperations = Substitute.For<IDocumentsOperations>();
            documentsOperations
                .SearchWithHttpMessagesAsync<SpecificationIndex>(Arg.Is<string>(_ => _ == $"\"{existingId}\""),
                    Arg.Is<SearchParameters>(_ => _.SearchFields.SequenceEqual(new[]
                    {
                        "id"
                    })))
                .Returns(Task.FromResult(documentSearchResult));

            ISearchServiceClient searchServiceClient = Substitute.For<ISearchServiceClient>();
            searchIndexClient.Documents.Returns(documentsOperations);

            SearchRepository<SpecificationIndex> searchRepository = new SearchRepository<SpecificationIndex>(searchRepositorySettings, searchInitializer, searchServiceClient, searchIndexClient);

            Func<Task<SpecificationIndex>> invocation = () => searchRepository.SearchById(existingId);

            invocation
                .Should()
                .Throw<FailedToQuerySearchException>();
        }
    }
}