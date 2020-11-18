using System.Data;

namespace CalculateFunding.Services.Publishing.SqlExport
{
    public interface IDataTableBuilder<in TDto>
    {
        DataTable DataTable { get; set; }
        string TableName { get; }
        bool HasNoData { get; }
        void AddRows(params TDto[] rows);
    }
}