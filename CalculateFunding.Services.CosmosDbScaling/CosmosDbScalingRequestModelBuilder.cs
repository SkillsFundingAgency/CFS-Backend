using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Models.CosmosDbScaling;
using CalculateFunding.Services.CosmosDbScaling.Interfaces;
using JobDefinitions = CalculateFunding.Services.Core.Constants.JobConstants.DefinitionNames;

namespace CalculateFunding.Services.CosmosDbScaling
{
    public class CosmosDbScalingRequestModelBuilder : ICosmosDbScalingRequestModelBuilder
    {
        public CosmosDbScalingRequestModel BuildRequestModel(JobNotification jobNotification)
        {
            CosmosDbScalingRequestModel cosmosDbScalingRequestModel = new CosmosDbScalingRequestModel
            {
                JobDefinitionId = jobNotification.JobType
            };

            cosmosDbScalingRequestModel.RepositoryTypes = jobNotification.JobType switch
            {
                JobDefinitions.CreateInstructAllocationJob => new[]
                {
                    CosmosCollectionType.CalculationProviderResults, CosmosCollectionType.ProviderSourceDatasets
                },
                JobDefinitions.CreateInstructGenerateAggregationsAllocationJob => new[]
                {
                    CosmosCollectionType.ProviderSourceDatasets
                },
                JobDefinitions.MapDatasetJob => new[]
                {
                    CosmosCollectionType.ProviderSourceDatasets
                },
                JobDefinitions.RefreshFundingJob => new[]
                {
                    CosmosCollectionType.PublishedFunding, CosmosCollectionType.CalculationProviderResults
                },
                JobDefinitions.PublishAllProviderFundingJob => new[]
                {
                    CosmosCollectionType.PublishedFunding
                },
                JobDefinitions.ApproveAllProviderFundingJob => new[]
                {
                    CosmosCollectionType.PublishedFunding
                },
                JobDefinitions.MergeSpecificationInformationForProviderJob => new[]
                {
                    CosmosCollectionType.CalculationProviderResults
                },
                JobDefinitions.DeleteCalculationsJob => new[]
                {
                    CosmosCollectionType.Calculations
                },
                JobDefinitions.DeleteCalculationResultsJob => new[]
                {
                    CosmosCollectionType.Calculations
                },
                JobDefinitions.AssignTemplateCalculationsJob => new[]
                {
                    CosmosCollectionType.Calculations
                },
                JobDefinitions.DeleteDatasetsJob => new []
                {
                    CosmosCollectionType.Datasets   
                },
                JobDefinitions.PublishBatchProviderFundingJob => new []
                {
                    CosmosCollectionType.PublishedFunding
                },
                JobDefinitions.ApproveBatchProviderFundingJob => new []
                {
                    CosmosCollectionType.PublishedFunding
                },
                JobDefinitions.DeletePublishedProvidersJob => new []
                {
                    CosmosCollectionType.PublishedFunding
                },
                JobDefinitions.PublishedFundingUndoJob => new []
                {
                    CosmosCollectionType.PublishedFunding
                },
                JobDefinitions.DeleteSpecificationJob => new []
                {
                    CosmosCollectionType.Specifications
                },
                JobDefinitions.DeleteTestResultsJob => new []
                {
                    CosmosCollectionType.TestResults
                },
                JobDefinitions.DeleteTestsJob => new []
                {
                    CosmosCollectionType.Tests
                },
                _ => cosmosDbScalingRequestModel.RepositoryTypes
            };

            return cosmosDbScalingRequestModel;
        }
    }
}
