using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Jobs;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.FeatureToggles;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Interfaces.ServiceBus;
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
        public async Task RegenerateProviderSourceDatasets_WhenUseMainJobServiceFeatureToggleOff_ThenQueueMessageDirectly()
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

            IMessengerService messengerService = CreateMessengerService();
            IJobsApiClient jobsApiClient = CreateJobsApiClient();

            DatasetService service = CreateDatasetService(datasetRepository: datasetRepository, messengerService: messengerService, jobsApiClient: jobsApiClient);

            // Act
            await service.RegenerateProviderSourceDatasets(request);

            // Assert
            await messengerService
                .Received(1)
                .SendToQueue(Arg.Is(ServiceBusConstants.QueueNames.ProcessDataset), Arg.Any<Dataset>(), Arg.Any<IDictionary<string, string>>());

            await jobsApiClient
                .DidNotReceive()
                .CreateJob(Arg.Any<JobCreateModel>());
        }

        [TestMethod]
        public async Task RegenerateProviderSourceDatasets_WhenUseMainJobServiceFeatureToggleOn_ThenCallJobServiceToProcess()
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

            IMessengerService messengerService = CreateMessengerService();
            IJobsApiClient jobsApiClient = CreateJobsApiClient();

            IFeatureToggle featureToggle = CreateFeatureToggle();
            featureToggle
                .IsJobServiceForMainActionsEnabled()
                .Returns(true);

            DatasetService service = CreateDatasetService(datasetRepository: datasetRepository, messengerService: messengerService, jobsApiClient: jobsApiClient, featureToggle: featureToggle);

            // Act
            await service.RegenerateProviderSourceDatasets(request);

            // Assert
            await messengerService
                .DidNotReceive()
                .SendToQueue(Arg.Is(ServiceBusConstants.QueueNames.ProcessDataset), Arg.Any<Dataset>(), Arg.Any<IDictionary<string, string>>());

            await jobsApiClient
                .Received(1)
                .CreateJob(Arg.Is<JobCreateModel>(j => j.JobDefinitionId == "MapDatasetJob"));
        }
    }
}
