using System.Collections.Generic;
using System.Data;
using System.Linq;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Profiling;

namespace CalculateFunding.Services.Publishing.SqlExport
{
    public class ProfilingDataTableBuilder : DataTableBuilder<PublishedProviderVersion>
    {
        private readonly uint _templateId;
        private readonly string _fundingLineCode;

        public ProfilingDataTableBuilder(uint templateId,
            string fundingLineCode)
        {
            _templateId = templateId;
            _fundingLineCode = fundingLineCode;
        }

        protected override DataColumn[] GetDataColumns(PublishedProviderVersion dto)
        {
            FundingLine fundingLine = dto.FundingLines.Single(_ => _.TemplateLineId == _templateId);
            ProfileTotal[] profiling = new PaymentFundingLineProfileTotals(dto, fundingLine.FundingLineCode)
                .ToArray();

            return new[]
                {
                    NewDataColumn<string>("PublishedProviderId", 128)
                }.Concat(profiling
                    .SelectMany(ProfilePeriodColumns))
                .ToArray();
        }

        private IEnumerable<DataColumn> ProfilePeriodColumns(ProfileTotal profile)
        {
            string profilePrefix = $"{profile.TypeValue}_{profile.PeriodType}_{profile.Year}_{profile.Occurrence}";

            return new[]
            {
                NewDataColumn<string>($"{profilePrefix}_Period", 64, true),
                NewDataColumn<string>($"{profilePrefix}_PeriodType", 64, true),
                NewDataColumn<string>($"{profilePrefix}_Year", 64, true),
                NewDataColumn<string>($"{profilePrefix}_Occurence", 64, true),
                NewDataColumn<string>($"{profilePrefix}_DistributionPeriod", 64, true),
                NewDataColumn<string>($"{profilePrefix}_Value", allowNull: true)
            };
        }

        private IEnumerable<object> ProfilePeriodRows(ProfileTotal profile)
            => new object[]
            {
                profile.TypeValue,
                profile.PeriodType,
                profile.Year,
                profile.Occurrence,
                profile.DistributionPeriod,
                profile.Value
            };

        protected override void AddDataRowToDataTable(PublishedProviderVersion dto)
        {
            FundingLine fundingLine = dto.FundingLines.Single(_ => _.TemplateLineId == _templateId);
            ProfileTotal[] profiling = new PaymentFundingLineProfileTotals(dto, fundingLine.FundingLineCode)
                .ToArray();

            DataTable.Rows.Add(new[]
            {
                dto.PublishedProviderId
            }.Concat(profiling.SelectMany(ProfilePeriodRows)).ToArray());
        }

        protected override void EnsureTableNameIsSet(PublishedProviderVersion dto)
            => TableName = $"[dbo].[{dto.FundingStreamId}_{dto.FundingPeriodId}_Profiles_{_fundingLineCode.Replace(" ", "")}]";
    }
}