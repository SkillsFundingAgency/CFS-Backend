using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Services.Datasets.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace CalculateFunding.Services.Datasets.Services
{
    [TestClass]
    public class ProcessDatasetServiceTests : ProcessDatasetServiceTestsBase
    {
       
        [TestMethod]
        public async Task GetDatasetAggregations_GivenSpecificationIdButNullAggregationsFound_ReturnsOKObjectWithEmptyList()
        {
            //Arrange
            IDatasetsAggregationsRepository datasetsAggregationsRepository = CreateDatasetsAggregationsRepository();
            datasetsAggregationsRepository
                .GetDatasetAggregationsForSpecificationId(Arg.Is(SpecificationId))
                .Returns((IEnumerable<DatasetAggregations>)null);

            ProcessDatasetService processDatasetService = CreateProcessDatasetService(datasetsAggregationsRepository: datasetsAggregationsRepository);

            //Act
            IActionResult result = await processDatasetService.GetDatasetAggregationsBySpecificationId(SpecificationId);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>()
                .Which
                .Value
                .Should()
                .BeOfType<DatasetAggregations[]>()
                .Which
                .Length
                .Should()
                .Be(0);
        }

        [TestMethod]
        public async Task GetDatasetAggregations_GivenSpecificationIdAndTwoAggregationsFound_ReturnsOKObjectWithTwoItems()
        {
            //Arrange
            IEnumerable<DatasetAggregations> datasetAggregations = new[]
            {
                new DatasetAggregations(),
                new DatasetAggregations()
            };

            IDatasetsAggregationsRepository datasetsAggregationsRepository = CreateDatasetsAggregationsRepository();
            datasetsAggregationsRepository
                .GetDatasetAggregationsForSpecificationId(Arg.Is(SpecificationId))
                .Returns(datasetAggregations);

            ProcessDatasetService processDatasetService = CreateProcessDatasetService(datasetsAggregationsRepository: datasetsAggregationsRepository);

            //Act
            IActionResult result = await processDatasetService.GetDatasetAggregationsBySpecificationId(SpecificationId);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>()
                .Which
                .Value
                .Should()
                .BeOfType<DatasetAggregations[]>()
                .Which
                .Length
                .Should()
                .Be(2);
        }
    }
}



