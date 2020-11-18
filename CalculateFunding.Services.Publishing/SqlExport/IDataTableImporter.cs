using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.SqlExport
{
    public interface IDataTableImporter
    {
        Task ImportDataTable<T>(IDataTableBuilder<T> dataTableBuilder);
    }
}