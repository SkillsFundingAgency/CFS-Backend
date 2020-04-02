using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Gherkin;

using CalculateFunding.Services.TestRunner.Interfaces;
using CalculateFunding.Services.TestRunner.Vocab.Calculation;
using Gherkin.Ast;
using Polly;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;

namespace CalculateFunding.Services.TestRunner.StepParsers
{
    public class ProviderStepParser : CalcStepParser, IStepParser
    {
        private readonly IProviderResultsRepository _providerResultsRepository;
        private readonly AsyncPolicy _providerResultsRepositoryPolicy;

        public ProviderStepParser(IProviderResultsRepository providerResultsRepository, ITestRunnerResiliencePolicies resiliencePolicies)
        {
            Guard.ArgumentNotNull(providerResultsRepository, nameof(providerResultsRepository));
            Guard.ArgumentNotNull(resiliencePolicies?.ProviderResultsRepository, nameof(providerResultsRepository));
            
            _providerResultsRepository = providerResultsRepository;
            _providerResultsRepositoryPolicy = resiliencePolicies.ProviderResultsRepository;
        }

        async public Task Parse(Step step, string stepExpression, GherkinParseResult parseResult, BuildProject buildProject)
        {
            string[] matches = Regex.Split(step.Text, stepExpression, RegexOptions.IgnoreCase);

            string comparison = matches[5];

            string providerId = matches[7];

            ProviderResult providerResult = await _providerResultsRepositoryPolicy.ExecuteAsync(() => _providerResultsRepository.GetProviderResultByProviderIdAndSpecificationId(providerId, buildProject.SpecificationId));
            if (providerResult == null)
            {
                parseResult.AddError($"Provider results for provider id : '{providerId}' could not be found", step.Location.Line, step.Location.Column);
            }

            parseResult.StepActions.Add(new AndProviderIs
            {
                ProviderId = providerId,
                Operator = ComparisonOperators.FirstOrDefault(x => x.Value == comparison).Key,
            });
        }

    }
}
