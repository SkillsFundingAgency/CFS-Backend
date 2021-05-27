using System.Linq;
using CalculateFunding.Api.Datasets.IntegrationTests.ConverterWizard;
using CalculateFunding.Common.Extensions;
using CalculateFunding.IntegrationTests.Common.Data;
using Microsoft.Extensions.Configuration;

namespace CalculateFunding.Api.Datasets.IntegrationTests.Data
{
    public class ConverterWizardActivityExcelBlobContext : ExcelBlobContext
    {
        private static readonly string[] Headers =
        {
            "Target UKPRN",
            "Target Provider Name",
            "Target Provider Status",
            "Target Opening Date",
            "Target Provider Ineligible",
            "Source Provider UKPRN"
        };

        public ConverterWizardActivityExcelBlobContext(IConfiguration configuration)
            : base(configuration,
                "converterwizardreports")
        {
        }

        protected override ExcelDocument GetExcelDocument(dynamic documentData)
        {
            ConverterActivityReportParameters converterActivityReportParameters = (ConverterActivityReportParameters) documentData;

            ExcelWorksheetData excelWorksheetData = new ExcelWorksheetData(converterActivityReportParameters.Name,
                Headers,
                converterActivityReportParameters.Rows.Select(_ => new object[]
                {
                    _.TargetUKPRN,
                    _.TargetProviderName,
                    _.TargetProviderStatus,
                    _.TargetOpeningDate,
                    _.TargetProviderIneligible,
                    _.SourceProviderUKPRN
                }).ToArray());

            return new ExcelDocument(converterActivityReportParameters.Path, CreateExcelFile(excelWorksheetData));
        }
    }
}