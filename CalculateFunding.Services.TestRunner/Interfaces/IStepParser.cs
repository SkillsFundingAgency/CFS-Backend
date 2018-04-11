using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Gherkin;
using Gherkin.Ast;
using System.Threading.Tasks;

namespace CalculateFunding.Services.TestRunner.Interfaces
{
    public interface IStepParser
    {
        Task Parse(Step step, string stepExpression, GherkinParseResult parseResult, BuildProject buildProject);
    }
}
