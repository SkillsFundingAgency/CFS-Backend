using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Results.SqlExport
{
    public interface ISqlImporter
    {
        Task ImportData(HashSet<string> providers, string specificationId);
    }
}
