using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.SqlExport
{
    public interface IQaRepository
    {
        SqlExportSource SqlExportSource { get; }

        int ExecuteSql(string sql);

        Task<IEnumerable<TableForStreamAndPeriod>> GetTablesForFundingStreamAndPeriod(string fundingStreamId,
            string fundingPeriodId);
    }
}