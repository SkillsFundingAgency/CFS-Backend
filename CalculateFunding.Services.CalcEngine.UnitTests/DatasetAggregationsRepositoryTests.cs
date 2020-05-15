using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using AutoMapper;
using CalculateFunding.Common.ApiClient.Calcs;
using CalculateFunding.Common.ApiClient.DataSets;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Models.Aggregations;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Services.CalcEngine;
using CalculateFunding.Services.CalcEngine.MappingProfiles;
using CalculateFunding.Services.Calcs.MappingProfiles;
using CalculateFunding.Services.Core;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace CalculateFunding.Services.Calculator
{
    [TestClass]
    public class DatasetAggregationsRepositoryTests
    {
        [TestMethod]
        public async Task GetDatasetAggregationsForSpecificationId_WhenSpeficationIdIsEmpty_ShouldThrowException()
        {
            // Arrange 
            IDatasetsApiClient datasetsApiClient = Substitute.For<IDatasetsApiClient>();
            IMapper mapper = CreateMapper();
            DatasetAggregationsRepository datasetAggregationsRepository = new DatasetAggregationsRepository(datasetsApiClient, CreateMapper());
            ArgumentNullException exception = null;

            // Act
            try
            {
                IEnumerable<DatasetAggregation> configuredTaskAwaiter = await datasetAggregationsRepository.GetDatasetAggregationsForSpecificationId(string.Empty);                
            }
            catch (Exception e)
            {
                exception = e as ArgumentNullException;
            }

            // Assert
            exception.Should().NotBeNull();
            exception.Should().BeOfType<ArgumentNullException>();
            await datasetsApiClient.DidNotReceive().GetDatasetAggregationsBySpecificationId(Arg.Any<string>());
        }

        [TestMethod]
        public async Task GetDatasetAggregationsForSpecificationId_WhenGivenASpecificationIdInValidFormat_ShouldReturnResult()
        {
            // Arrange           
            string _specificationId = "specificationId";
            IEnumerable<Common.ApiClient.DataSets.Models.DatasetAggregations> datasetAggregations = new List<Common.ApiClient.DataSets.Models.DatasetAggregations>()
            {
                new Common.ApiClient.DataSets.Models.DatasetAggregations()
                {
                    SpecificationId = _specificationId
                }
            };

            IDatasetsApiClient datasetsApiClient = Substitute.For<IDatasetsApiClient>();
            datasetsApiClient
                .GetDatasetAggregationsBySpecificationId(Arg.Any<string>())
                .Returns(new ApiResponse<IEnumerable<Common.ApiClient.DataSets.Models.DatasetAggregations>>(HttpStatusCode.OK, datasetAggregations));

            DatasetAggregationsRepository datasetAggregationsRepository = new DatasetAggregationsRepository(datasetsApiClient, CreateMapper());

            // Act
            IEnumerable<DatasetAggregation> result = await datasetAggregationsRepository.GetDatasetAggregationsForSpecificationId(_specificationId);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(datasetAggregations.Count());

            result.First().SpecificationId.Should().Be(datasetAggregations.First().SpecificationId);
            
            await datasetsApiClient.Received(1).GetDatasetAggregationsBySpecificationId(Arg.Any<string>());
        }

        [TestMethod]
        public async Task GetDatasetAggregationsForSpecificationId_WhenGivenASpecificationIdInValidFormat_ShouldReturnFail()
        {
            // Arrange           
            string _specificationId = "specificationId";
            IEnumerable<Common.ApiClient.DataSets.Models.DatasetAggregations> datasetAggregations = new List<Common.ApiClient.DataSets.Models.DatasetAggregations>()
            {
                new Common.ApiClient.DataSets.Models.DatasetAggregations()
                {
                    SpecificationId = _specificationId
                }
            };

            IDatasetsApiClient datasetsApiClient = Substitute.For<IDatasetsApiClient>();
            datasetsApiClient
                .GetDatasetAggregationsBySpecificationId(Arg.Any<string>())
                .Returns(new ApiResponse<IEnumerable<Common.ApiClient.DataSets.Models.DatasetAggregations>>(HttpStatusCode.NotFound, datasetAggregations));

            DatasetAggregationsRepository datasetAggregationsRepository = new DatasetAggregationsRepository(datasetsApiClient, CreateMapper());

            string errorMessage = $"No dataset aggregation for specification '{_specificationId}'";

            // Act
            
            Func<Task> result = async () => await datasetAggregationsRepository.GetDatasetAggregationsForSpecificationId(_specificationId);
            
        
            // Assert
            result
                .Should()
                .Throw<RetriableException>()
                .WithMessage(errorMessage);

            await datasetsApiClient.Received(1).GetDatasetAggregationsBySpecificationId(Arg.Any<string>());
        }


        [TestMethod]
        public async Task GetDatasetAggregationsForSpecificationId_WhenGivenApiResponseIsEmpty_ShouldReturnEmptyResult()
        {
            // Arrange 
            IDatasetsApiClient datasetsApiClient = Substitute.For<IDatasetsApiClient>();
            datasetsApiClient
                .GetDatasetAggregationsBySpecificationId(Arg.Any<string>())
                .Returns(new ApiResponse<IEnumerable<Common.ApiClient.DataSets.Models.DatasetAggregations>>(HttpStatusCode.OK, null));

            DatasetAggregationsRepository datasetAggregationsRepository = new DatasetAggregationsRepository(datasetsApiClient, CreateMapper());

            // Act
            IEnumerable<DatasetAggregation> result = await datasetAggregationsRepository.GetDatasetAggregationsForSpecificationId("Test");           

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(0);
            await datasetsApiClient.Received(1).GetDatasetAggregationsBySpecificationId(Arg.Any<string>());
        }

        private static IMapper CreateMapper()
        {
            MapperConfiguration mapperConfig = new MapperConfiguration(c =>
            {
                c.AddProfile<CalcEngineMappingProfile>();
            });

            return mapperConfig.CreateMapper();
        }
    }
}
