using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Results;
using CalculateFunding.Services.TestRunner.Interfaces;
using Gherkin.Ast;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CalculateFunding.Services.TestRunner.StepParsers
{
    public class ProviderStepParser : IStepParser
    {
        private readonly IProviderResultsRepository _providerResultsRepository;

        public ProviderStepParser(IProviderResultsRepository providerResultsRepository)
        {
            _providerResultsRepository = providerResultsRepository;
        }

        async public Task Parse(Step step, string stepExpression, GherkinParseResult parseResult, BuildProject buildProject)
        {
            string[] matches = Regex.Split(step.Text, stepExpression, RegexOptions.IgnoreCase);

            string providerId = matches[7];

            ProviderResult providerResult = await _providerResultsRepository.GetProviderByIdAndSpecificationId(providerId, buildProject.Specification.Id);

            if(providerResult == null)
            {
                parseResult.AddError($"Provider results for provider id : '{providerId}' could not be found", step.Location.Line, step.Location.Column);
            }
        }
    }
}
