using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Jobs;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Services.Datasets.Interfaces;
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
            IDatasetRepository datasetRepository = CreateDatasetsRepository();
            DefinitionSpecificationRelationship relationship = NewDefinitionSpecificationRelationship(_ =>
                            _.WithCurrent(NewDefinitionSpecificationRelationshipVersion(v => v
                                            .WithDatasetVersion(new DatasetRelationshipVersion { Id = "DSRV1", Version = 1 })
                                            .WithSpecification(new Reference { Id = SpecificationId, Name = "SpecAbc" }))));

            datasetRepository
                .GetDefinitionSpecificationRelationshipsByQuery(Arg.Any<Expression<Func<DocumentEntity<DefinitionSpecificationRelationship>, bool>>>())
                .Returns(new List<DefinitionSpecificationRelationship>{ relationship });
            datasetRepository
                .GetDatasetsByQuery(Arg.Any<Expression<Func<DocumentEntity<Dataset>, bool>>>())
                .Returns(new List<Dataset>
                {
                    new Dataset { Id = "DS1"}
                });

            IJobManagement jobManagement = CreateJobManagement();

            DatasetService service = CreateDatasetService(datasetRepository: datasetRepository, jobManagement: jobManagement);

            // Act
            await service.RegenerateProviderSourceDatasets(SpecificationId, null, null);

            // Assert
            await jobManagement
                .Received(1)
                .QueueJob(Arg.Is<JobCreateModel>(j => 
                j.JobDefinitionId == "MapDatasetJob" && 
                j.Properties.ContainsKey("session-id") && 
                j.Properties["session-id"] == SpecificationId));
        }
    }
}
