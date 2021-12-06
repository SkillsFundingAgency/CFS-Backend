using System.Collections.Generic;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.SqlExport;

namespace CalculateFunding.Services.Publishing.SqlExport
{
    public interface ISqlImportContext
    {
        SchemaContext SchemaContext { get; set; }
        ICosmosDbFeedIterator Documents { get; set; }
        IDataTableBuilder<PublishedProviderVersion> Providers { get; set; }
        IDataTableBuilder<PublishedProviderVersion> Funding { get; set; }
        IDictionary<uint, IDataTableBuilder<PublishedProviderVersion>> Profiling { get; set; }
        IDataTableBuilder<PublishedProviderVersion> PaymentFundingLines { get; set; }
        IDataTableBuilder<PublishedProviderVersion> InformationFundingLines { get; set; }
        IDataTableBuilder<PublishedProviderVersion> Calculations { get; set; }
        IDictionary<uint, string> CalculationNames { get; set; }
        void AddRows(PublishedProviderVersion dto);
    }
}