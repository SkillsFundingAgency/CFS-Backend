using CalculateFunding.Services.SqlExport;
using System.Threading.Tasks;

namespace CalculateFunding.Services.SqlExport
{
    public interface IDataTableImporter
    {
        Task ImportDataTable<T>(IDataTableBuilder<T> dataTableBuilder);
    }
}