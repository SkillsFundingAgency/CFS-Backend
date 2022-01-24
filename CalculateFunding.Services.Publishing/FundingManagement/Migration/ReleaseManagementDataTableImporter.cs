using CalculateFunding.Common.Sql.Interfaces;
using CalculateFunding.Services.Publishing.FundingManagement.Interfaces;
using CalculateFunding.Services.SqlExport;

namespace CalculateFunding.Services.Publishing.FundingManagement.Migration
{
    public class ReleaseManagementDataTableImporter : DataTableImporter, IReleaseManagementDataTableImporter, IDataTableImporter
    {
        public ReleaseManagementDataTableImporter(ISqlConnectionFactory sqlConnectionFactory) : base(sqlConnectionFactory)
        {
        }
    }
}
