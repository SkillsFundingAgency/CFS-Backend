using System;
using System.Data;
using CalculateFunding.Models.Publishing;

namespace CalculateFunding.Services.Publishing.SqlExport
{
    public class ProviderDataTableBuilder : DataTableBuilder<PublishedProviderVersion>
    {
        protected override DataColumn[] GetDataColumns(PublishedProviderVersion dto) =>
            new[]
            {
                NewDataColumn<string>("PublishedProviderId", 128),
                NewDataColumn<string>("ProviderId", 32),
                NewDataColumn<string>("ProviderName", 256),
                NewDataColumn<string>("UKPRN", 32),
                NewDataColumn<string>("URN", 32),
                NewDataColumn<string>("Authority", 256),
                NewDataColumn<string>("ProviderType", 128),
                NewDataColumn<string>("ProviderSubType", 128),
                NewDataColumn<DateTime>("DateOpened", allowNull: true),
                NewDataColumn<DateTime>("DateClosed", allowNull: true),
                NewDataColumn<string>("LaCode", 32),
                NewDataColumn<string>("Status", 64, true),
                NewDataColumn<string>("Successor", 32, true),
                NewDataColumn<string>("TrustCode", 32),
                NewDataColumn<string>("TrustName", 128),
                NewDataColumn<string>("PaymentOrganisationIdentifier", 32, true),
                NewDataColumn<string>("PaymentOrganisationName", 256, true)
            };

        protected override void AddDataRowToDataTable(PublishedProviderVersion dto)
        {
            Provider provider = dto.Provider;

            DataTable.Rows.Add(dto.PublishedProviderId,
                dto.ProviderId,
                provider.Name,
                provider.UKPRN,
                provider.URN,
                provider.Authority,
                provider.ProviderType,
                provider.ProviderSubType,
                DbNullSafe(provider.DateOpened?.UtcDateTime),
                DbNullSafe(provider.DateClosed?.UtcDateTime),
                provider.LACode,
                DbNullSafe(provider.Status),
                DbNullSafe(provider.Successor),
                provider.TrustCode,
                provider.TrustName,
                DbNullSafe(provider.PaymentOrganisationIdentifier),
                DbNullSafe(provider.PaymentOrganisationName));
        }

        protected override void EnsureTableNameIsSet(PublishedProviderVersion dto)
            => TableName = $"[dbo].[{dto.FundingStreamId}_{dto.FundingPeriodId}_Providers]";
    }
}