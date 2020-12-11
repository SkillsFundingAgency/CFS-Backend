using System;
using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Models.Publishing;

namespace CalculateFunding.Services.Publishing.SqlExport
{
    public class SqlImportContext : ISqlImportContext
    {
        public ICosmosDbFeedIterator<PublishedProvider> Documents { get; set; }

        public IDataTableBuilder<PublishedProviderVersion> Providers { get; set; }

        public IDataTableBuilder<PublishedProviderVersion> Funding { get; set; }

        public IDictionary<uint, IDataTableBuilder<PublishedProviderVersion>> Profiling { get; set; }

        public IDataTableBuilder<PublishedProviderVersion> PaymentFundingLines { get; set; }

        public IDataTableBuilder<PublishedProviderVersion> InformationFundingLines { get; set; }

        public IDataTableBuilder<PublishedProviderVersion> Calculations { get; set; }

        public IDictionary<uint, string> CalculationNames { get; set; }

        public void AddRows(PublishedProviderVersion dto)
        {
            Providers.AddRows(dto);
            Funding.AddRows(dto);
            PaymentFundingLines.AddRows(dto);
            InformationFundingLines.AddRows(dto);
            Calculations.AddRows(dto);

            EnsureProfilingIsSetUp(dto);

            foreach (FundingLine fundingLine in dto.FundingLines ?? ArraySegment<FundingLine>.Empty)
            {
                if (Profiling.TryGetValue(fundingLine.TemplateLineId, out IDataTableBuilder<PublishedProviderVersion> profiling))
                {
                    profiling.AddRows(dto);
                }
            }
        }

        private void EnsureProfilingIsSetUp(PublishedProviderVersion dto)
        {
            if (Profiling != null)
            {
                return;
            }

            lock (this)
            {
                if (Profiling != null)
                {
                    return;
                }

                Profiling = dto.FundingLines?.Where(_ => _.Type == FundingLineType.Payment)
                    .ToDictionary(_ => _.TemplateLineId,
                        _ => (IDataTableBuilder<PublishedProviderVersion>)
                            new ProfilingDataTableBuilder(_.TemplateLineId, _.FundingLineCode)) ?? new Dictionary<uint, IDataTableBuilder<PublishedProviderVersion>>();
            }
        }
    }
}