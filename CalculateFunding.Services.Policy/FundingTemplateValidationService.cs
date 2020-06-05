using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.Extensions;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Common.Storage;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Policy;
using CalculateFunding.Services.Policy.Interfaces;
using CalculateFunding.Services.Policy.Models;
using FluentValidation.Results;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;

namespace CalculateFunding.Services.Policy
{
    public class FundingTemplateValidationService : IFundingTemplateValidationService, IHealthChecker
    {
        private const string fundingSchemaFolder = "funding";

        private readonly IFundingSchemaRepository _fundingSchemaRepository;
        private readonly Polly.AsyncPolicy _fundingSchemaRepositoryPolicy;
        private readonly IPolicyRepository _policyRepository;
        private readonly Polly.AsyncPolicy _policyRepositoryPolicy;

        public FundingTemplateValidationService(
            IFundingSchemaRepository fundingSchemaRepository,
            IPolicyResiliencePolicies policyResiliencePolicies,
            IPolicyRepository policyRepository)
        {
            Guard.ArgumentNotNull(fundingSchemaRepository, nameof(fundingSchemaRepository));
            Guard.ArgumentNotNull(policyResiliencePolicies?.FundingSchemaRepository, nameof(policyResiliencePolicies.FundingSchemaRepository));
            Guard.ArgumentNotNull(policyResiliencePolicies?.PolicyRepository, nameof(policyResiliencePolicies.PolicyRepository));
            Guard.ArgumentNotNull(policyRepository, nameof(policyRepository));

            _fundingSchemaRepository = fundingSchemaRepository;
            _fundingSchemaRepositoryPolicy = policyResiliencePolicies.FundingSchemaRepository;
            _policyRepository = policyRepository;
            _policyRepositoryPolicy = policyResiliencePolicies.PolicyRepository;
        }

        public async Task<ServiceHealth> IsHealthOk()
        {
            (bool Ok, string Message) fundingSchemaRepoHealth = await ((BlobClient) _fundingSchemaRepository).IsHealthOk();

            ServiceHealth health = new ServiceHealth()
            {
                Name = nameof(FundingSchemaService)
            };

            health.Dependencies.Add(new DependencyHealth
            {
                HealthOk = fundingSchemaRepoHealth.Ok,
                DependencyName = fundingSchemaRepoHealth.GetType().GetFriendlyName(),
                Message = fundingSchemaRepoHealth.Message
            });

            return health;
        }

        public async Task<FundingTemplateValidationResult> ValidateFundingTemplate(string fundingTemplate)
        {
            Guard.IsNullOrWhiteSpace(fundingTemplate, nameof(fundingTemplate));

            FundingTemplateValidationResult fundingTemplateValidationResult = new FundingTemplateValidationResult();

            JObject parsedFundingTemplate;

            try
            {
                parsedFundingTemplate = JObject.Parse(fundingTemplate);
            }
            catch (JsonReaderException jre)
            {
                fundingTemplateValidationResult.Errors.Add(new ValidationFailure("", jre.Message));

                return fundingTemplateValidationResult;
            }

            if (parsedFundingTemplate["schemaVersion"] == null ||
                string.IsNullOrWhiteSpace(parsedFundingTemplate["schemaVersion"].Value<string>()))
            {
                fundingTemplateValidationResult.Errors.Add(new ValidationFailure("", "Missing schema version from funding template."));

                return fundingTemplateValidationResult;
            }

            string schemaVersion = parsedFundingTemplate["schemaVersion"].Value<string>();
            fundingTemplateValidationResult.SchemaVersion = schemaVersion;

            string blobName = $"{fundingSchemaFolder}/{schemaVersion}.json";

            bool schemaExists = await _fundingSchemaRepositoryPolicy.ExecuteAsync(() => _fundingSchemaRepository.SchemaVersionExists(blobName));

            if (!schemaExists)
            {
                fundingTemplateValidationResult.Errors.Add(new ValidationFailure("",
                    $"A valid schema could not be found for schema version '{schemaVersion}'."));

                return fundingTemplateValidationResult;
            }

            await ValidateAgainstSchema(blobName, parsedFundingTemplate, fundingTemplateValidationResult);

            if (!fundingTemplateValidationResult.IsValid)
            {
                return fundingTemplateValidationResult;
            }

            await ExtractFundingStreamAndVersion(fundingTemplateValidationResult, parsedFundingTemplate);

            return fundingTemplateValidationResult;
        }

        private async Task ValidateAgainstSchema(string blobName, JObject parsedFundingTemplate,
            FundingTemplateValidationResult fundingTemplateValidationResult)
        {
            string fundingSchemaJson =
                await _fundingSchemaRepositoryPolicy.ExecuteAsync(() => _fundingSchemaRepository.GetFundingSchemaVersion(blobName));

            JSchema fundingSchema = JSchema.Parse(fundingSchemaJson);

            bool isValidTemplate = parsedFundingTemplate.IsValid(fundingSchema, out IList<string> validationMessages);

            if (!isValidTemplate)
            {
                foreach (string message in validationMessages)
                {
                    fundingTemplateValidationResult.Errors.Add(new ValidationFailure("", message));
                }
            }
        }

        private async Task ExtractFundingStreamAndVersion(FundingTemplateValidationResult fundingTemplateValidationResult,
            JObject parsedFundingTemplate)
        {
            var fundingStreamCode = parsedFundingTemplate.SelectToken("$..fundingStream.code")?.Value<string>();
            if (!fundingStreamCode.IsNullOrEmpty())
            {
                fundingTemplateValidationResult.FundingStreamId = fundingStreamCode;

                FundingStream fundingStream = await _policyRepositoryPolicy.ExecuteAsync(() =>
                    _policyRepository.GetFundingStreamById(fundingTemplateValidationResult.FundingStreamId));

                if (fundingStream == null)
                {
                    fundingTemplateValidationResult.Errors
                        .Add(new ValidationFailure("", $"A funding stream could not be found for funding stream id '{fundingTemplateValidationResult.FundingStreamId}'"));
                }
            }

            string fundingPeriodId = parsedFundingTemplate.SelectToken("$..fundingPeriod.id")?.Value<string>();

            if(!string.IsNullOrWhiteSpace(fundingPeriodId))
            {
                fundingTemplateValidationResult.FundingPeriodId = fundingPeriodId;

                FundingPeriod fundingPeriod = await _policyRepositoryPolicy.ExecuteAsync(() => _policyRepository.GetFundingPeriodById(fundingTemplateValidationResult.FundingPeriodId));

                if(fundingPeriod == null)
                {
                    fundingTemplateValidationResult.Errors
                        .Add(new ValidationFailure("", $"A funding period could not be found for funding period id '{fundingTemplateValidationResult.FundingPeriodId}'"));
                }
            }

            var templateVersion = parsedFundingTemplate.SelectToken("$..templateVersion", false)?.Value<string>();

            if (!templateVersion.IsNullOrEmpty())
            {
                fundingTemplateValidationResult.TemplateVersion = templateVersion;
            }

            var schemaVersionToken = parsedFundingTemplate.SelectToken("$..schemaVersion")?.Value<string>();

            if (schemaVersionToken.IsNullOrEmpty())
            {
                fundingTemplateValidationResult.Errors.Add(new ValidationFailure("", "No schemaVersion property found"));
            }
            else
            {
                fundingTemplateValidationResult.SchemaVersion = schemaVersionToken;
            }
        }
    }
}
