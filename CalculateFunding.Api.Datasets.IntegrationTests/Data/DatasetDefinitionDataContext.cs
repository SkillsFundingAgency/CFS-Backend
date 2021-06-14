using System.Reflection;
using CalculateFunding.IntegrationTests.Common.Data;
using CalculateFunding.Models.Datasets.Schema;
using CalculateFunding.Services.Core.Extensions;
using Microsoft.Extensions.Configuration;

namespace CalculateFunding.Api.Datasets.IntegrationTests.Data
{
    public class DatasetDefinitionDataContext : NoPartitionKeyCosmosBulkDataContext
    {
        public DatasetDefinitionDataContext(IConfiguration configuration,
            Assembly resourceAssembly) : base(configuration,
            "datasets",
            "CalculateFunding.Api.Datasets.IntegrationTests.Resources.DatasetDefinitionTemplate",
            resourceAssembly)
        {
        }

        protected override object GetFormatParametersForDocument(dynamic documentData,
            string now)
        {
            string tableDefinitions = ((TableDefinition[]) documentData.TableDefinitions).AsJson().Prettify();
            
            return new
            {
                ID = documentData.Id,
                NAME = documentData.Name,
                FUNDINGSTREAMID = documentData.FundingStreamId,
                DESCRIPTION = documentData.Description,
                VERSION = documentData.Version,
                CONVERTERENABLED = documentData.ConverterEnabled.ToString().ToLower(),
                CONVERTERELIGIBLE = documentData.ConverterEligible.ToString().ToLower(),
                TABLEDEFINITIONS = tableDefinitions,
                NOW = now
            };
        }
    }
}