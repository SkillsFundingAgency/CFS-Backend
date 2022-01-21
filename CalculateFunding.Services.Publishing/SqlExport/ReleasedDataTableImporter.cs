using CalculateFunding.Common.Sql.Interfaces;
using CalculateFunding.Services.SqlExport;

namespace CalculateFunding.Services.Publishing.SqlExport
{
    public class ReleasedDataTableImporter : DataTableImporter, IPublishingDataTableImporter
    {
        public ReleasedDataTableImporter(ISqlConnectionFactory sqlConnectionFactory) : base(sqlConnectionFactory)
        {
        }

        public SqlExportSource SqlExportSource => SqlExportSource.ReleasedPublishedProviderVersion;
    }
}
