using CalculateFunding.Models.Calcs;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Datasets.Interfaces
{
    public interface ICalcsRepository
    {
        Task<BuildProject> GetBuildProjectBySpecificationId(string specificationId);
    }
}
