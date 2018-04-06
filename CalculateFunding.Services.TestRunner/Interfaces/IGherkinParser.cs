using System.Threading.Tasks;
using CalculateFunding.Models.Calcs;

namespace CalculateFunding.Services.TestRunner
{
    public interface IGherkinParser
    {
        Task<GherkinParseResult> Parse(string gherkin, BuildProject buildProject);
    }
}
