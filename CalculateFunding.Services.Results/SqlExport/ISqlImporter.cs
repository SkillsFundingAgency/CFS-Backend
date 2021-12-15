using System.Threading.Tasks;

namespace CalculateFunding.Services.Results.SqlExport
{
    public interface ISqlImporter
    {
        Task ImportData(string specificationId);
    }
}
