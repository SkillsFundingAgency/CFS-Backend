using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Results.SqlExport
{
    public interface IQaRepository
    {
        int ExecuteSql(string sql);

        Task<IEnumerable<TableForSpecification>> GetTablesForSpecification(string specificationId);
    }
}
