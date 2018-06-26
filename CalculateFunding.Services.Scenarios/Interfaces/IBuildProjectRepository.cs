using CalculateFunding.Models.Calcs;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Scenarios.Interfaces
{
    public interface IBuildProjectRepository
    {
        Task<BuildProject> GetBuildProjectBySpecificationId(string specificationId);
    }
}
