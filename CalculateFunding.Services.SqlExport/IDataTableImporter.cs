using System.Data.SqlClient;
using System.Threading.Tasks;

namespace CalculateFunding.Services.SqlExport
{
    public interface IDataTableImporter
    {
        Task ImportDataTable<T>(IDataTableBuilder<T> dataTableBuilder, SqlBulkCopyOptions sqlBulkCopyOptions = SqlBulkCopyOptions.Default);
    }
}