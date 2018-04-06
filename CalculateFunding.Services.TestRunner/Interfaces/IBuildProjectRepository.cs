using CalculateFunding.Models.Calcs;
using System.Threading.Tasks;

namespace CalculateFunding.Services.TestRunner.Interfaces
{
    public interface IBuildProjectRepository
    {
        Task<BuildProject> GetBuildProjectBySpecificationId(string specificationId);
    }
}
