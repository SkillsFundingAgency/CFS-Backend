using CalculateFunding.IntegrationTests.Common.Data;
using Microsoft.Extensions.Configuration;
using System.Reflection;

namespace CalculateFunding.Api.Results.IntegrationTests.Data
{
    public class CalculationDataContext : NoPartitionKeyCosmosBulkDataContext
    {
        public CalculationDataContext(IConfiguration configuration,
            Assembly resourceAssembly) : base(configuration,
            "specs",
            "CalculateFunding.Api.Results.IntegrationTests.Resources.CalculationTemplate",
            resourceAssembly)
        {
        }

        protected override object GetFormatParametersForDocument(dynamic documentData,
            string now) =>
            new
            {
                ID = documentData.Id,
                SPECIFICATIONID = documentData.SpecificationId,
                CALCULATIONTYPE = documentData.CalculationType,
                CALCULATIONVALUETYPE = documentData.CalculationValueType,
                CALCULATIONDATATYPE = documentData.CalculationDataType,
                FUNDINGSTREAMID = documentData.FundingStreamId,
                NAME = documentData.Name,
                NOW = now
            };
    }
}
