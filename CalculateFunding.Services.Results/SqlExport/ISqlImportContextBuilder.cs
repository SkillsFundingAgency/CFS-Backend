using System.Threading.Tasks;

namespace CalculateFunding.Services.Results.SqlExport
{
    public interface ISqlImportContextBuilder
    {
        Task<ISqlImportContext> CreateImportContext(string specificationId);
    }
}
