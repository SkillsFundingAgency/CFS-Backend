using System.Threading.Tasks;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Gherkin;

namespace CalculateFunding.Services.TestRunner
{
    public interface IGherkinParser
    {
        Task<GherkinParseResult> Parse(string specificationId, string gherkin, BuildProject buildProject);
    }
}
