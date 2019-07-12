using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Jobs;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Services.Datasets.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.Extensions.Primitives;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace CalculateFunding.Services.Datasets.Services
{
    [TestClass]
    public partial class DatasetServiceRegenerateProviderSourceDatasetsTests : DatasetServiceTestsBase
    {
        [TestMethod]
        public async Task RegenerateProviderSourceDatasets_GivenSpecificationId_ThenCallJobServiceToProcess()
        {
            // Arrange
            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "specificationId", new StringValues(SpecificationId) },
            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request.Query.Returns(queryStringValues);

            IDatasetRepository datasetRepository = CreateDatasetsRepository();
            datasetRepository
                .GetDefinitionSpecificationRelationshipsByQuery(Arg.Any<Expression<Func<DefinitionSpecificationRelationship, bool>>>())
                .Returns(new List<DefinitionSpecificationRelationship>
                {
                    new DefinitionSpecificationRelationship{
                        DatasetVersion = new DatasetRelationshipVersion { Id = "DSRV1", Version = 1},
                        Specification = new Common.Models.Reference { Id = SpecificationId, Name = "SpecAbc"}
                    }
                });
            datasetRepository
                .GetDatasetsByQuery(Arg.Any<Expression<Func<Dataset, bool>>>())
                .Returns(new List<Dataset>
                {
                    new Dataset { Id = "DS1"}
                });

            IJobsApiClient jobsApiClient = CreateJobsApiClient();

            DatasetService service = CreateDatasetService(datasetRepository: datasetRepository, jobsApiClient: jobsApiClient);

            // Act
            await service.RegenerateProviderSourceDatasets(request);

            // Assert
            await jobsApiClient
                .Received(1)
                .CreateJob(Arg.Is<JobCreateModel>(j => 
                j.JobDefinitionId == "MapDatasetJob" && 
                j.Properties.ContainsKey("session-id") && 
                j.Properties["session-id"] == SpecificationId));
        }
    }
}
