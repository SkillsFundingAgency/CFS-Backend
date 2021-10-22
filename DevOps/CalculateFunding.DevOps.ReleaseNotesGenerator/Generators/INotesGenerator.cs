using CalculateFunding.DevOps.ReleaseNotesGenerator.Options;
using System.Threading.Tasks;

namespace CalculateFunding.DevOps.ReleaseNotesGenerator.Generators
{
    public interface INotesGenerator
    {
        Task Generate(ConsoleOptions azureDevOpsOptions);
    }
}
