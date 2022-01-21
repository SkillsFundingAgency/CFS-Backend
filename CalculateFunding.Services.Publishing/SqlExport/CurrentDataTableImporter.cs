using CalculateFunding.Common.Sql.Interfaces;
using CalculateFunding.Services.SqlExport;

namespace CalculateFunding.Services.Publishing.SqlExport
{
    public class CurrentDataTableImporter : DataTableImporter, IPublishingDataTableImporter
    {
        public CurrentDataTableImporter(ISqlConnectionFactory sqlConnectionFactory) : base(sqlConnectionFactory)
        {
        }

        public SqlExportSource SqlExportSource => SqlExportSource.CurrentPublishedProviderVersion;
    }
}
