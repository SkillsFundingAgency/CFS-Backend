using System;
using System.Data;
using System.Linq;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.SqlExport;

namespace CalculateFunding.Services.Publishing.SqlExport
{
    public class ProviderPaymentFundingLineDataTableBuilder : DataTableBuilder<PublishedProviderVersion>
    {
        protected override DataColumn[] GetDataColumns(PublishedProviderVersion dto)
        {
            FundingLine[] paymentFundingLines = dto.FundingLines.Where(_ => _.Type == FundingLineType.Payment)
                .OrderBy(_ => _.TemplateLineId)
                .ToArray();

            return new[]
            {
                NewDataColumn<string>("PublishedProviderId", 128),
                NewDataColumn<string>("ProviderId", 32),
                NewDataColumn<string>("MajorVersion", 32),
                NewDataColumn<string>("FundingLineCode", 32),
                NewDataColumn<string>("FundingLineName", 64),
                NewDataColumn<decimal>("FundingValue"),
                NewDataColumn<bool>("IsIndicative", defaultValue: false),
                NewDataColumn<DateTime>("LastUpdated"),
                NewDataColumn<string>("LastUpdatedBy", 256)
            }
            .ToArray();
        }

        protected override void AddDataRowToDataTable(PublishedProviderVersion dto)
        {
            if(dto.FundingLines == null)
            {
                return;
            }

            FundingLine[] paymentFundingLines = dto.FundingLines.Where(_ => _.Type == FundingLineType.Payment)
                .Where(_ => _.Value.HasValue)
                .OrderBy(_ => _.TemplateLineId)
                .ToArray();

            foreach (FundingLine paymentFundingLine in paymentFundingLines)
            {
                DataTable.Rows.Add(
                    dto.PublishedProviderId,
                    dto.ProviderId,
                    dto.MajorVersion.ToString(),
                    DbNullSafe(paymentFundingLine.FundingLineCode),
                    DbNullSafe(paymentFundingLine.Name),
                    DbNullSafe(paymentFundingLine.Value),
                    dto.IsIndicative,
                    dto.Date.UtcDateTime,
                    dto.Author.Name
                );
            }
        }

        protected override void EnsureTableNameIsSet(PublishedProviderVersion dto)
            => TableName = $"[dbo].[{dto.FundingStreamId}_{dto.FundingPeriodId}_ProviderPaymentFundingLinesAllVersions]";
    }
}