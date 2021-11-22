using System.Linq;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Models.CosmosDbScaling;
using CalculateFunding.Services.Core.Constants;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.CosmosDbScaling
{
    [TestClass]
    public class CosmosDbScalingRequestModelBuilderTests
    {
        [TestMethod]
        public void BuildRequestModel_GivenJobNotificationWithDefinitionNotConfiguredForScaling_ContainsNoRepositoryTypes()
        {
            //Arrange
            JobSummary jobNotification = new JobSummary
            {
                JobType = "any-job-def-id"
            };

            CosmosDbScalingRequestModelBuilder builder = new CosmosDbScalingRequestModelBuilder();

            //Act
            CosmosDbScalingRequestModel requestModel = builder.BuildRequestModel(jobNotification);

            //Assert
            requestModel
                .RepositoryTypes
                .Should()
                .BeNull();
        }

#if NCRUNCH
        [Ignore]
#endif
        [TestMethod]
        [DataRow(JobConstants.DefinitionNames.CreateInstructAllocationJob,
            new[] { CosmosCollectionType.CalculationProviderResults, CosmosCollectionType.ProviderSourceDatasets })]
        [DataRow(JobConstants.DefinitionNames.PopulateCalculationResultsQaDatabaseJob,
            new[] { CosmosCollectionType.CalculationProviderResults})]
        [DataRow(JobConstants.DefinitionNames.CreateInstructGenerateAggregationsAllocationJob,
            new[] { CosmosCollectionType.ProviderSourceDatasets })]
        [DataRow(JobConstants.DefinitionNames.MapDatasetJob,
            new[] { CosmosCollectionType.ProviderSourceDatasets })]
        [DataRow(JobConstants.DefinitionNames.RefreshFundingJob,
            new[] { CosmosCollectionType.PublishedFunding, CosmosCollectionType.CalculationProviderResults })]
        [DataRow(JobConstants.DefinitionNames.PublishAllProviderFundingJob,
            new[] { CosmosCollectionType.PublishedFunding })]
        [DataRow(JobConstants.DefinitionNames.ApproveAllProviderFundingJob,
            new[] { CosmosCollectionType.PublishedFunding })]
        [DataRow(JobConstants.DefinitionNames.DeleteCalculationResultsJob,
            new[] { CosmosCollectionType.Calculations })]
        [DataRow(JobConstants.DefinitionNames.DeleteCalculationsJob,
            new[] { CosmosCollectionType.Calculations })]
        [DataRow(JobConstants.DefinitionNames.AssignTemplateCalculationsJob,
            new[] { CosmosCollectionType.Calculations })]
        [DataRow(JobConstants.DefinitionNames.DeleteDatasetsJob,
            new[] { CosmosCollectionType.Datasets })]
        [DataRow(JobConstants.DefinitionNames.PublishBatchProviderFundingJob,
            new[] { CosmosCollectionType.PublishedFunding })]
        [DataRow(JobConstants.DefinitionNames.ApproveBatchProviderFundingJob,
            new[] { CosmosCollectionType.PublishedFunding })]
        [DataRow(JobConstants.DefinitionNames.DeletePublishedProvidersJob,
            new[] { CosmosCollectionType.PublishedFunding })]
        [DataRow(JobConstants.DefinitionNames.PublishedFundingUndoJob,
            new[] { CosmosCollectionType.PublishedFunding })]
        [DataRow(JobConstants.DefinitionNames.DeleteSpecificationJob,
            new[] { CosmosCollectionType.Specifications })]
        [DataRow(JobConstants.DefinitionNames.ApproveAllCalculationsJob,
            new[] { CosmosCollectionType.Calculations })]
        public void BuildRequestModel_GivenJobWithDefinitions_EnsuresCorrectRepositoryTypes(string jobDefinitionId,
            CosmosCollectionType[] cosmosRepositoryTypes)
        {
            //Arrange
            JobSummary jobNotification = new JobSummary
            {
                JobType = jobDefinitionId
            };

            CosmosDbScalingRequestModelBuilder builder = new CosmosDbScalingRequestModelBuilder();

            //Act
            CosmosDbScalingRequestModel requestModel = builder.BuildRequestModel(jobNotification);

            //Assert
            requestModel
                 .RepositoryTypes
                 .SequenceEqual(cosmosRepositoryTypes)
                 .Should()
                 .BeTrue();
        }
    }
}
