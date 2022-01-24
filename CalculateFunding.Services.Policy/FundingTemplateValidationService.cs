using System.Collections.Generic;
using System.IO;
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
using NJsonSchema;
using NJsonSchema.Validation;

namespace CalculateFunding.Services.Policy
{
    public class FundingTemplateValidationService : IFundingTemplateValidationService, IHealthChecker
    {
        private const string fundingSchemaFolder = "funding";

        private readonly IFundingSchemaRepository _fundingSchemaRepository;
        private readonly Polly.AsyncPolicy _fundingSchemaRepositoryPolicy;
        private readonly IPolicyRepository _policyRepository;
        private readonly Polly.AsyncPolicy _policyRepositoryPolicy;
        private readonly IFundingSchemaVersionParseService _fundingSchemaVersionParseService;

        public FundingTemplateValidationService(
            IFundingSchemaRepository fundingSchemaRepository,
            IPolicyResiliencePolicies policyResiliencePolicies,
            IPolicyRepository policyRepository,
            IFundingSchemaVersionParseService fundingSchemaVersionParseService)
        {
            Guard.ArgumentNotNull(fundingSchemaRepository, nameof(fundingSchemaRepository));
            Guard.ArgumentNotNull(policyResiliencePolicies?.FundingSchemaRepository, nameof(policyResiliencePolicies.FundingSchemaRepository));
            Guard.ArgumentNotNull(policyResiliencePolicies?.PolicyRepository, nameof(policyResiliencePolicies.PolicyRepository));
            Guard.ArgumentNotNull(policyRepository, nameof(policyRepository));

            _fundingSchemaRepository = fundingSchemaRepository;
            _fundingSchemaRepositoryPolicy = policyResiliencePolicies.FundingSchemaRepository;
            _policyRepository = policyRepository;
            _policyRepositoryPolicy = policyResiliencePolicies.PolicyRepository;
            _fundingSchemaVersionParseService = fundingSchemaVersionParseService;
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

        public async Task<FundingTemplateValidationResult> ValidateFundingTemplate(string fundingTemplate, string fundingStreamId, string fundingPeriodId, string templateVersion = null)
        {
            Guard.IsNullOrWhiteSpace(fundingTemplate, nameof(fundingTemplate));

            FundingTemplateValidationResult fundingTemplateValidationResult = new FundingTemplateValidationResult()
            {
                FundingPeriodId = fundingPeriodId,
                FundingStreamId = fundingStreamId,
                TemplateVersion = templateVersion,
            };

            string schemaVersion = _fundingSchemaVersionParseService.GetInputTemplateSchemaVersion(fundingTemplate);

            if (string.IsNullOrWhiteSpace(schemaVersion))
            {
                fundingTemplateValidationResult.Errors.Add(new ValidationFailure("", "Missing schema version from funding template."));

                return fundingTemplateValidationResult;
            }

            fundingTemplateValidationResult.SchemaVersion = schemaVersion;

            string blobName = $"{fundingSchemaFolder}/{schemaVersion}.json";

            bool schemaExists = await _fundingSchemaRepositoryPolicy.ExecuteAsync(() => _fundingSchemaRepository.SchemaVersionExists(blobName));

            if (!schemaExists)
            {
                fundingTemplateValidationResult.Errors.Add(new ValidationFailure("", $"A valid schema could not be found for schema version '{schemaVersion}'."));

                return fundingTemplateValidationResult;
            }

            await ValidateAgainstSchema(blobName, fundingTemplate, fundingTemplateValidationResult);

            if (!fundingTemplateValidationResult.IsValid)
            {
                return fundingTemplateValidationResult;
            }

            await ValidateFundingStream(fundingTemplateValidationResult);
            await ValidateFundingPeriod(fundingTemplateValidationResult);

            return fundingTemplateValidationResult;
        }

        private async Task ValidateAgainstSchema(string blobName, string fundingTemplate,
            FundingTemplateValidationResult fundingTemplateValidationResult)
        {
            string fundingSchemaJson =
                await _fundingSchemaRepositoryPolicy.ExecuteAsync(() => _fundingSchemaRepository.GetFundingSchemaVersion(blobName));
            JsonSchema fundingSchema = await JsonSchema.FromJsonAsync(fundingSchemaJson);

            using (StringReader reader = new StringReader(fundingTemplate))
            {
                using (JsonTextReader jsonReader = new JsonTextReader(reader)
                {
                    DateParseHandling = DateParseHandling.None,
                    MaxDepth = 1024
                })
                {

                    JToken jsonObject = JToken.ReadFrom(jsonReader);

                    ICollection<ValidationError> validationMessages = fundingSchema.Validate(jsonObject);
                    if (validationMessages.AnyWithNullCheck())
                    {
                        foreach (ValidationError message in validationMessages)
                        {
                            fundingTemplateValidationResult.Errors.Add(new ValidationFailure(message.Property, message.ToString()));
                        }
                    }
                }
            }
        }

        private async Task ValidateFundingStream(FundingTemplateValidationResult fundingTemplateValidationResult)
        {
            if (!fundingTemplateValidationResult.FundingStreamId.IsNullOrEmpty())
            {
                FundingStream fundingStream = await _policyRepositoryPolicy.ExecuteAsync(() =>
                    _policyRepository.GetFundingStreamById(fundingTemplateValidationResult.FundingStreamId));

                if (fundingStream == null)
                {
                    fundingTemplateValidationResult.Errors
                        .Add(new ValidationFailure("", $"A funding stream could not be found for funding stream id '{fundingTemplateValidationResult.FundingStreamId}'"));
                }
            }
        }

        private async Task ValidateFundingPeriod(FundingTemplateValidationResult fundingTemplateValidationResult)
        {
            if (!string.IsNullOrWhiteSpace(fundingTemplateValidationResult.FundingPeriodId))
            {
                FundingPeriod fundingPeriod = await _policyRepositoryPolicy.ExecuteAsync(() => _policyRepository.GetFundingPeriodById(fundingTemplateValidationResult.FundingPeriodId));

                if (fundingPeriod == null)
                {
                    fundingTemplateValidationResult.Errors
                        .Add(new ValidationFailure("", $"A funding period could not be found for funding period id '{fundingTemplateValidationResult.FundingPeriodId}'"));
                }
            }
        }
    }
}
