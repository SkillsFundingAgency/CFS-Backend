using System;
using System.Threading.Tasks;
using CalculateFunding.Models.Calcs;

namespace CalculateFunding.Services.TestRunner.Interfaces
{
    [Obsolete("Replace with common nuget API client")]
    public interface IBuildProjectRepository
    {
        Task<BuildProject> GetBuildProjectBySpecificationId(string specificationId);
    }
}
