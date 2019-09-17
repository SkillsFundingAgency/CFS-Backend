using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Models.CosmosDbScaling;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.CosmosDbScaling.Interfaces;

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

            switch (jobNotification.JobType)
            {
                case JobConstants.DefinitionNames.CreateInstructAllocationJob:
                    cosmosDbScalingRequestModel.RepositoryTypes = new[]
                    {
                        CosmosCollectionType.CalculationProviderResults,
                        CosmosCollectionType.ProviderSourceDatasets
                    };
                    break;

                case JobConstants.DefinitionNames.CreateInstructGenerateAggregationsAllocationJob:
                    cosmosDbScalingRequestModel.RepositoryTypes = new[]
                    {
                        CosmosCollectionType.ProviderSourceDatasets
                    };
                    break;

                case JobConstants.DefinitionNames.MapDatasetJob:
                    cosmosDbScalingRequestModel.RepositoryTypes = new[]
                    {
                        CosmosCollectionType.ProviderSourceDatasets
                    };
                    break;

                case JobConstants.DefinitionNames.PublishProviderResultsJob:
                    cosmosDbScalingRequestModel.RepositoryTypes = new[]
                    {
                        CosmosCollectionType.CalculationProviderResults,
                        CosmosCollectionType.PublishedProviderResults
                    };
                    break;
                case JobConstants.DefinitionNames.RefreshFundingJob:
                case JobConstants.DefinitionNames.PublishFundingJob:
                case JobConstants.DefinitionNames.ApproveFunding:
                    cosmosDbScalingRequestModel.RepositoryTypes = new[]
                    {
                        CosmosCollectionType.PublishedFundingResults,
                    };
                    break;

            }

            return cosmosDbScalingRequestModel;
        }
    }
}
