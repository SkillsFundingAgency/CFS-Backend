using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using CalculateFunding.Common.ApiClient.Profiling.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Publishing.Profiling;
using CalculateFunding.Services.SqlExport;

namespace CalculateFunding.Services.Publishing.SqlExport
{
    public class ProfilingDataTableBuilder : DataTableBuilder<PublishedProviderVersion>
    {
        private readonly uint _templateId;
        private readonly string _fundingLineCode;
        private readonly ProfilePeriodPattern[] _profilePeriodPatterns;
        private readonly ProfilePeriodsMap _profilePeriodsMap;

        public ProfilingDataTableBuilder(uint templateId,
            string fundingLineCode,
            params ProfilePeriodPattern[] profilePeriodPatterns)
        {
            _templateId = templateId;
            _fundingLineCode = fundingLineCode;
            _profilePeriodPatterns = profilePeriodPatterns;
            _profilePeriodsMap = new ProfilePeriodsMap(profilePeriodPatterns);
        }

        protected override DataColumn[] GetDataColumns(PublishedProviderVersion dto) =>
            new[]
                {
                    NewDataColumn<string>("PublishedProviderId", 128)
                }.Concat(_profilePeriodPatterns
                    .SelectMany(ProfilePeriodColumns))
                .Concat(new [] { NewDataColumn<string>("Carry_Over_Value", allowNull: true) })
                .ToArray();

        private IEnumerable<DataColumn> ProfilePeriodColumns(ProfilePeriodPattern profile)
        {
            string profilePrefix = $"{profile.Period}_{profile.PeriodType}_{profile.PeriodYear}_{profile.Occurrence}";

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

        private IEnumerable<object> ProfilePeriodColumnValues(ProfileTotal[] profiles)
            => _profilePeriodsMap.GetProfilePeriodValues(profiles)
                .SelectMany(_ => _)
                .ToArray();

        protected override void AddDataRowToDataTable(PublishedProviderVersion dto)
        {
            FundingLine fundingLine = dto.FundingLines.Single(_ => _.TemplateLineId == _templateId);
            ProfileTotal[] profiling = new PaymentFundingLineProfileTotals(dto, fundingLine.FundingLineCode)
                .ToArray();

            object[] profilePeriods = ProfilePeriodColumnValues(profiling).ToArray();

            int templatePeriodCount = DataTable.Columns.Count - 2;

            if (templatePeriodCount != profilePeriods.Length)
            {
                throw new NonRetriableException($"Unable to import data into table {TableName} for funding line code {fundingLine.FundingLineCode}.\n Funding" +
                                                $" line for provider {dto.ProviderId} has {profilePeriods.Length/6} profile periods but the template expected {templatePeriodCount/6}");
            }

            IEnumerable<ProfilingCarryOver> profilingCarryOvers = dto?.CarryOvers?.Where(_ => _.FundingLineCode == fundingLine.FundingLineCode);
            if (profilingCarryOvers?.Count() > 1)
            {
                throw new InvalidOperationException($"Provider {dto?.Provider?.UKPRN} has multiple carry over values for {fundingLine.FundingLineCode}");
            }

            object[] fundingLineCarryOver = { profilingCarryOvers?.SingleOrDefault()?.Amount };
            
            DataTable.Rows.Add(new object[]
            {
                dto.PublishedProviderId
            }.Concat(profilePeriods).Concat(fundingLineCarryOver).ToArray());
        }

        protected override void EnsureTableNameIsSet(PublishedProviderVersion dto)
            => TableName = $"[dbo].[{dto.FundingStreamId}_{dto.FundingPeriodId}_Profiles_{_fundingLineCode.Replace(" ", "")}]";
    }
}