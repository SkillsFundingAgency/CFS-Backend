using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Services.SqlExport;
using System;
using System.Data;
using System.Linq;

namespace CalculateFunding.Services.Results.SqlExport
{
    public class CalculationRunDataTableBuilder : DataTableBuilder<ProviderResult>
    {
        private readonly SpecificationSummary _specificationSummary;
        private readonly JobSummary _jobSummary;

        private readonly object lockObject = new object();

        public CalculationRunDataTableBuilder(
            SpecificationSummary specificationSummary,
            JobSummary jobSummary)
        {
            _specificationSummary = specificationSummary;
            _jobSummary = jobSummary;
        }

        protected override DataColumn[] GetDataColumns(ProviderResult dto)
            => new[]
            {
                NewDataColumn<string>("SpecificationId", 128),
                NewDataColumn<string>("FundingStreamId", 32),
                NewDataColumn<string>("FundingPeriodId", 32),
                NewDataColumn<string>("SpecificationName", 128),
                NewDataColumn<string>("TemplateVersion", 128),
                NewDataColumn<string>("ProviderVersion", 128),
                NewDataColumn<DateTime>("LastUpdated", allowNull: true),
                NewDataColumn<string>("LastUpdatedBy", 256, allowNull: true),
            };

        protected override void AddDataRowToDataTable(ProviderResult dto)
        {
            if(DataTable.Rows.Count == 1)
            {
                return;
            }

            lock (lockObject)
            {
                if (DataTable.Rows.Count == 1)
                {
                    return;
                }

                DataTable.Rows.Add(
                    dto.SpecificationId,
                    _specificationSummary.FundingStreams.FirstOrDefault().Id,
                    _specificationSummary.FundingPeriod.Id,
                    _specificationSummary.Name,
                    _specificationSummary.TemplateIds[_specificationSummary.FundingStreams.FirstOrDefault().Id],
                    _specificationSummary.ProviderVersionId,
                    _jobSummary.LastUpdated.UtcDateTime,
                    _jobSummary.InvokerUserDisplayName
                    );
            }
        }

        protected override void EnsureTableNameIsSet(ProviderResult dto)
            => TableName = $"[dbo].[{dto.SpecificationId}_CalculationRun]";
    }
}
