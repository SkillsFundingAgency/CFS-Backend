using System;
using System.Reflection;
using CalculateFunding.IntegrationTests.Common.Data;
using Microsoft.Extensions.Configuration;

namespace CalculateFunding.Api.Datasets.IntegrationTests.Data
{
    public class ConverterWizardJobLogDataContext : NoPartitionKeyCosmosBulkDataContext
    {
        public ConverterWizardJobLogDataContext(IConfiguration configuration,
            Assembly resourceAssembly) : base(configuration,
            "datasets",
            "CalculateFunding.Api.Datasets.IntegrationTests.Resources.ConverterDataMergeLog",
            resourceAssembly)
        {
        }

        protected override object GetFormatParametersForDocument(dynamic documentData,
            string now) =>
            throw new NotImplementedException("TODO; currently only for clean up");
    }
}