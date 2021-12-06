using System.Threading.Tasks;

namespace CalculateFunding.Services.Results.SqlExport
{
    public interface IQaSchemaService
    {
        Task EnsureSqlTablesForSpecification(string specificationId);

        Task ReCreateTablesForSpecification(string specificationId);
    }
}
