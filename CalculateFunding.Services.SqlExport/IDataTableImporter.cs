using Microsoft.Data.SqlClient;
using System.Threading.Tasks;
using CalculateFunding.Common.Sql.Interfaces;

namespace CalculateFunding.Services.SqlExport
{
    public interface IDataTableImporter
    {
        Task ImportDataTable<T>(IDataTableBuilder<T> dataTableBuilder, SqlBulkCopyOptions sqlBulkCopyOptions = SqlBulkCopyOptions.Default, ISqlTransaction transaction = null);
    }
}