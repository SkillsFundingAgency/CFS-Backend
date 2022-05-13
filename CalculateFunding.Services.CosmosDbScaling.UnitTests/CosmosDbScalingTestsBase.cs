using CalculateFunding.Common.Extensions;
using CalculateFunding.Models.CosmosDbScaling;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.CosmosDbScaling
{
    public class CosmosDbScalingTestsBase
    {
        protected IEnumerable<CosmosDbScalingConfig> cosmosDbScalingConfig;

        [TestInitialize]
        public void Setup()
        {
            string json = @"[
            {
                'id': 'e91f53db-c57d-4afc-b56e-583a48b5c40e',
                'repositoryType': 'CalculationProviderResults',
                'jobRequestUnitConfigs': [
                    {
                        'jobDefinitionId': 'CreateInstructAllocationJob',
                        'jobRequestUnits': 200000
                    },
                    {
                        'jobDefinitionId': 'PublishProviderResultsJob',
                        'jobRequestUnits': 50000
                    },
                    {
                        'jobDefinitionId': 'RefreshFundingJob',
                        'jobRequestUnits': 100000
                    },
                    {
                        'jobDefinitionId': 'MergeSpecificationInformationForProviderJob',
                        'jobRequestUnits': 50000
                    },
                    {
                        'jobDefinitionId': 'GenerateCalcCsvResultsJob',
                        'jobRequestUnits': 5000
                    },
                    {
                        'jobDefinitionId': 'PopulateCalculationResultsQaDatabaseJob',
                        'jobRequestUnits': 200000
                    }
                ]
            },
            {
                'id': '78c4348c-05d1-4a8c-832a-16780eab3be0',
                'repositoryType': 'Calculations',
                'jobRequestUnitConfigs': [
                    {
                        'jobDefinitionId': 'DeleteCalculationResultsJob',
                        'jobRequestUnits': 50000
                    },
                    {
                        'jobDefinitionId': 'DeleteCalculationsJob',
                        'jobRequestUnits': 50000
                    },
                    {
                        'jobDefinitionId': 'AssignTemplateCalculationsJob',
                        'jobRequestUnits': 3400
                    },
                    {
                        'jobDefinitionId': 'ApproveAllCalculationsJob',
                        'jobRequestUnits': 3400
                    }
                ]
            },
            {
                'id': '2f649fa2-840c-45e0-93ac-5a560ba0007b',
                'repositoryType': 'Datasets',
                'jobRequestUnitConfigs': [
                    {
                        'jobDefinitionId': 'DeleteDatasetsJob',
                        'jobRequestUnits': 50000
                    }
                ]
            },
            {
                'id': '4891433b-f859-42c7-acf9-f35b771ff8a1',
                'repositoryType': 'Jobs',
                'jobRequestUnitConfigs': [
                    {
                        'jobDefinitionId': 'DeleteJobsJob',
                        'jobRequestUnits': 50000
                    },
                    {
                        'jobDefinitionId': 'MergeSpecificationInformationForProviderJob',
                        'jobRequestUnits': 100
                    }
                ]
            },
            {
                'id': '4876de8f-b45e-44a0-86b5-d5305408f568',
                'repositoryType': 'ProviderSourceDatasets',
                'jobRequestUnitConfigs': [
                    {
                        'jobDefinitionId': 'MapDatasetJob',
                        'jobRequestUnits': 45000
                    },
                    {
                        'jobDefinitionId': 'CreateInstructAllocationJob',
                        'jobRequestUnits': 50000
                    },
                    {
                        'jobDefinitionId': 'CreateInstructGenerateAggregationsAllocationJob',
                        'jobRequestUnits': 50000
                    }
                ]
            },
            {
                'id': 'b159fa26-99ba-4180-bf54-2837db3c9657',
                'repositoryType': 'PublishedFunding',
                'jobRequestUnitConfigs': [
                    {
                        'jobDefinitionId': 'RefreshFundingJob',
                        'jobRequestUnits': 250000
                    },
                    {
                        'jobDefinitionId': 'ReleaseProvidersToChannelsJob',
                        'jobRequestUnits': 250000
                    },
                    {
                        'jobDefinitionId': 'PublishAllProviderFundingJob',
                        'jobRequestUnits': 250000
                    },
                    {
                        'jobDefinitionId': 'ApproveAllProviderFundingJob',
                        'jobRequestUnits': 250000
                    },
                    {
                        'jobDefinitionId': 'PublishBatchProviderFundingJob',
                        'jobRequestUnits': 250000
                    },
                    {
                        'jobDefinitionId': 'ApproveBatchProviderFundingJob',
                        'jobRequestUnits': 250000
                    },
                    {
                        'jobDefinitionId': 'PublishIntegrityCheckJob',
                        'jobRequestUnits': 250000
                    },
                    {
                        'jobDefinitionId': 'DeletePublishedProvidersJob',
                        'jobRequestUnits': 150000
                    },
                    {
                        'jobDefinitionId': 'PublishedFundingUndoJob',
                        'jobRequestUnits': 150000
                    },
                    {
                        'jobDefinitionId': 'RunSqlImportJob',
                        'jobRequestUnits': 150000
                    },
                    {
                        'jobDefinitionId': 'BatchPublishedProviderValidationJob',
                        'jobRequestUnits': 150000
                    },
                    {
                        'jobDefinitionId': 'GeneratePublishedProviderEstateCsvJob',
                        'jobRequestUnits': 1000
                    },
                    {
                        'jobDefinitionId': 'GeneratePublishedFundingCsvJob',
                        'jobRequestUnits': 1000
                    },
                    {
                        'jobDefinitionId': 'GeneratePublishedProviderStateSummaryCsvJob',
                        'jobRequestUnits': 1000
                    },
                    {
                        'jobDefinitionId': 'ReleaseManagementDataMigrationJob',
                        'jobRequestUnits': 300000
                    }
                ]
            },
            {
                'id': '510fa41a-d11d-4a07-b34f-99200b999f9d',
                'repositoryType': 'Specifications',
                'jobRequestUnitConfigs': [
                    {
                        'jobDefinitionId': 'DeleteSpecificationJob',
                        'jobRequestUnits': 50000
                    },
                    {
                        'jobDefinitionId': 'DeleteSpecificationCleanUpJob',
                        'jobRequestUnits': 50000
                    }
                ]
            }
            ]";
            cosmosDbScalingConfig = json.AsPoco<IEnumerable<CosmosDbScalingConfig>>();
        }
    }
}
