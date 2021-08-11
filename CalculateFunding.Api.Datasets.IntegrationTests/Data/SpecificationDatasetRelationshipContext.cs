using System.Reflection;
using CalculateFunding.Common.ApiClient.DataSets.Models;
using CalculateFunding.Common.Extensions;
using CalculateFunding.IntegrationTests.Common.Data;
using Microsoft.Extensions.Configuration;

namespace CalculateFunding.Api.Datasets.IntegrationTests.Data
{
    public class SpecificationDatasetRelationshipContext : NoPartitionKeyCosmosBulkDataContext
    {
        public SpecificationDatasetRelationshipContext(IConfiguration configuration,
            Assembly resourceAssembly)
            : base(configuration,
                "datasets",
                "CalculateFunding.Api.Datasets.IntegrationTests.Resources.DefinitionSpecificationRelationship",
                resourceAssembly)
        {
        }

        protected override object GetFormatParametersForDocument(dynamic documentData,
            string now) =>
            new
            {
                ID = documentData.Id,
                DESCRIPTION = documentData.Description,
                NAME = documentData.Name,
                DEFINITIONID = documentData.DefinitionId,
                DEFINITIONNAME = documentData.DefinitionName,
                DATASETID = documentData.DatasetId,
                DATASETNAME = documentData.DatasetName,
                CONVERTERENABLED = documentData.ConverterEnabled.ToString().ToLower(),
                SPECIFICATIONID = documentData.SpecificationId,
                SPECIFICATIONNAME = documentData.SpecificationName,
                PUBLISHEDSPECIFICATIONCONFIURATION = new PublishedSpecificationConfiguration { 
                    SpecificationId = documentData.PublishedSpecificationConfiguration.SpecificationId,
                    FundingPeriodId = documentData.PublishedSpecificationConfiguration.FundingPeriodId,
                    FundingStreamId = documentData.PublishedSpecificationConfiguration.FundingStreamId,
                    Calculations = documentData.PublishedSpecificationConfiguration.Calculations,
                    FundingLines = documentData.PublishedSpecificationConfiguration.FundingLines
                }.AsJson(),
                DATASETRELATIONSHIPTYPE = documentData.DatasetRelationshipType.ToString(),
                NOW = now
            };
    }
}