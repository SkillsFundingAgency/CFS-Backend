using System.Linq;
using CalculateFunding.Api.Datasets.IntegrationTests.ConverterWizard;
using CalculateFunding.Common.Extensions;
using CalculateFunding.IntegrationTests.Common.Data;
using Microsoft.Extensions.Configuration;

namespace CalculateFunding.Api.Datasets.IntegrationTests.Data
{
    public class ProviderDatasetExcelBlobContext : ExcelBlobContext
    {
        private static readonly string[] Headers =
        {
            "Ukprn",
            "Status",
            "Name",
            "ProviderType",
            "ProviderSubType",
            "Predecessors",
            "Successors"
        };

        public ProviderDatasetExcelBlobContext(IConfiguration configuration)
            : base(configuration,
                "datasets")
        {
        }

        protected override ExcelDocument GetExcelDocument(dynamic documentData)
        {
            ProviderDatasetParameters providerDatasetParameter = (ProviderDatasetParameters) documentData;

            ExcelWorksheetData excelWorksheetData = new ExcelWorksheetData("FundingStreamName",
                Headers,
                providerDatasetParameter.Rows.Select(_ => new object[]
                {
                    _.Ukprn,
                    _.Status,
                    _.Name,
                    _.ProviderType,
                    _.ProviderSubType,
                    _.Predecessors?.JoinWith(','),
                    _.Successors?.JoinWith(',')
                }).ToArray());

            return new ExcelDocument(providerDatasetParameter.Path, CreateExcelFile(excelWorksheetData));
        }
    }
}