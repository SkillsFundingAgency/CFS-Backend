using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.Extensions;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Common.Storage;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Policy;
using CalculateFunding.Services.Policy.Interfaces;
using CalculateFunding.Services.Policy.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;

namespace CalculateFunding.Services.Policy
{
    public class FundingTemplateValidationService : IFundingTemplateValidationService, IHealthChecker
    {
        private const string fundingSchemaFolder = "funding";

        private readonly IFundingSchemaRepository _fundingSchemaRepository;
        private readonly Polly.Policy _fundingSchemaRepositoryPolicy;
        private readonly IPolicyRepository _policyRepository;
        private readonly Polly.Policy _policyRepositoryPolicy;

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
            (bool Ok, string Message) fundingSchemaRepoHealth = await ((BlobClient)_fundingSchemaRepository).IsHealthOk();

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
                fundingTemplateValidationResult.ValidationState.Errors.Add(jre.Message);

                return fundingTemplateValidationResult;
            }

            if (parsedFundingTemplate["schemaVersion"] == null ||
                string.IsNullOrWhiteSpace(parsedFundingTemplate["schemaVersion"].Value<string>()))
            {
                fundingTemplateValidationResult.ValidationState.Errors.Add("Missing schema version from funding template.");

                return fundingTemplateValidationResult;
            }

            string schemaVersion = parsedFundingTemplate["schemaVersion"].Value<string>();

            string blobName = $"{fundingSchemaFolder}/{schemaVersion}.json";

            bool schemaExists = await _fundingSchemaRepositoryPolicy.ExecuteAsync(() => _fundingSchemaRepository.SchemaVersionExists(blobName));

            if (!schemaExists)
            {
                fundingTemplateValidationResult.ValidationState.Errors.Add($"A valid schema could not be found for schema version '{schemaVersion}'.");

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
    
        private async Task ValidateAgainstSchema(string blobName, JObject parsedFundingTemplate, FundingTemplateValidationResult fundingTemplateValidationResult)
        {
            string fundingSchemaJson = await _fundingSchemaRepositoryPolicy.ExecuteAsync(() => _fundingSchemaRepository.GetFundingSchemaVersion(blobName));

            JSchema fundingSchema = JSchema.Parse(fundingSchemaJson);

            bool isValidTemplate = parsedFundingTemplate.IsValid(fundingSchema, out IList<string> validationMessages);

            if (!isValidTemplate)
            {
                foreach (string message in validationMessages)
                {
                    fundingTemplateValidationResult.ValidationState.Errors.Add(message);
                }
            }
        }

        private async Task ExtractFundingStreamAndVersion(FundingTemplateValidationResult fundingTemplateValidationResult, JObject parsedFundingTemplate)
        {
            if (parsedFundingTemplate["funding"] == null)
            {
                fundingTemplateValidationResult.ValidationState.Errors.Add("No funding property found");

                return;
            }

            if (parsedFundingTemplate["funding"]["fundingStream"] == null)
            {
                fundingTemplateValidationResult.ValidationState.Errors.Add("No funding stream property found");

                return;
            }

            if (parsedFundingTemplate["funding"]["fundingStream"]["code"] == null)
            {
                fundingTemplateValidationResult.ValidationState.Errors.Add("No funding stream code property found");

                return;
            }

            if (parsedFundingTemplate["funding"]["templateVersion"] == null)
            {
                fundingTemplateValidationResult.ValidationState.Errors.Add("No template version property found");

                return;
            }

            if (string.IsNullOrWhiteSpace(parsedFundingTemplate["funding"]["fundingStream"]["code"].Value<string>()))
            {
                fundingTemplateValidationResult.ValidationState.Errors.Add("Funding stream id is missing from the template");
            }
            else
            {
                fundingTemplateValidationResult.FundingStreamId = parsedFundingTemplate["funding"]["fundingStream"]["code"].Value<string>();

                FundingStream fundingStream = await _policyRepositoryPolicy.ExecuteAsync(() => _policyRepository.GetFundingStreamById(fundingTemplateValidationResult.FundingStreamId));

                if (fundingStream == null)
                {
                    fundingTemplateValidationResult.ValidationState.Errors.Add($"A funding stream could not be found for funding stream id '{fundingTemplateValidationResult.FundingStreamId}'");
                }
            }

            string templateVersion = fundingTemplateValidationResult.Version = parsedFundingTemplate["funding"]["templateVersion"].Value<string>();

            if (string.IsNullOrWhiteSpace(templateVersion))
            {
                fundingTemplateValidationResult.ValidationState.Errors.Add("Funding template version is missing from the template");
            }
            else
            {
                fundingTemplateValidationResult.Version = templateVersion;
            }
        }
    }
}
