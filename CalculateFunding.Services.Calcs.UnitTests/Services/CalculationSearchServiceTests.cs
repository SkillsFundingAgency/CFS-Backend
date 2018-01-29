using CalculateFunding.Models;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Repositories.Common.Search;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Search.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using NSubstitute;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Calcs.Services
{
    [TestClass]
    public class CalculationSearchServiceTests
    {
        [TestMethod]
        public async Task SearchCalculation_GivenNullSearchModel_LogsAndCreatesDefaultSearcModel()
        {
            //Arrange
            HttpRequest request = Substitute.For<HttpRequest>();

            ILogger logger = CreateLogger();

            ISearchRepository<CalculationIndex> searchRepository = CreateSearchRepository();

            CalculationSearchService service = CreateCalculationSearchService(logger: logger, serachRepository: searchRepository);

            //Act
            IActionResult result = await service.SearchCalculations(request);

            //Assert
            logger
                .Received(1)
                .Error("A null or invalid search model was provided for searching calculations");

            result
                 .Should()
                 .BeOfType<BadRequestObjectResult>();
        }

        [TestMethod]
        public async Task SearchCalculation_GivenPageNumberIsZero_LogsAndReturnsBadRequest()
        {
            //Arrange
            SearchModel model = new SearchModel();
            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Body
                .Returns(stream);

            ILogger logger = CreateLogger();

            ISearchRepository<CalculationIndex> searchRepository = CreateSearchRepository();

            CalculationSearchService service = CreateCalculationSearchService(logger: logger, serachRepository: searchRepository);

            //Act
            IActionResult result = await service.SearchCalculations(request);

            //Assert
            logger
                .Received(1)
                .Error("A null or invalid search model was provided for searching calculations");

            result
                 .Should()
                 .BeOfType<BadRequestObjectResult>();
        }

        [TestMethod]
        public async Task SearchCalculation_GivenPageTopIsZero_LogsAndReturnsBadRequest()
        {
            //Arrange
            SearchModel model = new SearchModel
            {
                PageNumber = 1,
                Top = 0
            };

            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Body
                .Returns(stream);

            ILogger logger = CreateLogger();

            ISearchRepository<CalculationIndex> searchRepository = CreateSearchRepository();

            CalculationSearchService service = CreateCalculationSearchService(logger: logger, serachRepository: searchRepository);

            //Act
            IActionResult result = await service.SearchCalculations(request);

            //Assert
            logger
                .Received(1)
                .Error("A null or invalid search model was provided for searching calculations");

            result
                 .Should()
                 .BeOfType<BadRequestObjectResult>();
        }

        [TestMethod]
        public async Task SearchCalculation_GivenValidModel_CallsSearch()
        {
            //Arrange
            SearchModel model = new SearchModel
            {
                PageNumber = 1,
                Top = 50
            };

            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Body
                .Returns(stream);

            SearchResults<CalculationIndex> searchResults = new SearchResults<CalculationIndex>();

            ILogger logger = CreateLogger();

            ISearchRepository<CalculationIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .Search(Arg.Any<string>(), Arg.Any<SearchParameters>())
                .Returns(searchResults);
                
            CalculationSearchService service = CreateCalculationSearchService(logger: logger, serachRepository: searchRepository);

            //Act
            IActionResult result = await service.SearchCalculations(request);

            //Assert
            result
                 .Should()
                 .BeOfType<OkObjectResult>();

            await
                searchRepository
                    .Received(7)
                    .Search(Arg.Any<string>(), Arg.Any<SearchParameters>());
                
        }

        static CalculationSearchService CreateCalculationSearchService(
           ILogger logger = null, ISearchRepository<CalculationIndex> serachRepository = null)
        {
            return new CalculationSearchService(
                logger ?? CreateLogger(), serachRepository ?? CreateSearchRepository());
        }

        static ILogger CreateLogger()
        {
            return Substitute.For<ILogger>();
        }

        static ISearchRepository<CalculationIndex> CreateSearchRepository()
        {
            return Substitute.For<ISearchRepository<CalculationIndex>>();
        }
    }
}
