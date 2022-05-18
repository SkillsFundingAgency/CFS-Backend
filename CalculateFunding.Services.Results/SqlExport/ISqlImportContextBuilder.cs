using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Results.SqlExport
{
    public interface ISqlImportContextBuilder
    {
        Task<ISqlImportContext> CreateImportContext(string specificationId, HashSet<string> providers);
    }
}
