using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Common.Storage;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Policy.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using Serilog;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Policy
{
    public class FundingSchemaService : IFundingSchemaService, IHealthChecker
    {
        private const string fundingSchemaFolder = "funding";

        private readonly ILogger _logger;
        private readonly IFundingSchemaRepository _fundingSchemaRepository;
        private readonly Polly.Policy _fundingSchemaRepositoryPolicy;

        public FundingSchemaService(
            ILogger logger,
            IFundingSchemaRepository fundingSchemaRepository,
            IPolicyResiliencePolicies policyResiliencePolicies)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(fundingSchemaRepository, nameof(fundingSchemaRepository));
            Guard.ArgumentNotNull(policyResiliencePolicies?.FundingSchemaRepository, nameof(policyResiliencePolicies.FundingSchemaRepository));

            _logger = logger;
            _fundingSchemaRepository = fundingSchemaRepository;
            _fundingSchemaRepositoryPolicy = policyResiliencePolicies.FundingSchemaRepository;
        }

        public async Task<ServiceHealth> IsHealthOk()
        {
            (bool Ok, string Message) fundingSchemaRepoHealth = await ((BlobClient)_fundingSchemaRepository).IsHealthOk();

            ServiceHealth health = new ServiceHealth()
            {
                Name = nameof(FundingSchemaService)
            };

            health.Dependencies.Add(new DependencyHealth { HealthOk = fundingSchemaRepoHealth.Ok, DependencyName = fundingSchemaRepoHealth.GetType().GetFriendlyName(), Message = fundingSchemaRepoHealth.Message });

            return health;
        }

        public async Task<IActionResult> GetFundingSchemaByVersion(string schemaVersion)
        {
            Guard.IsNullOrWhiteSpace(schemaVersion, nameof(schemaVersion));

            string blobName = $"{fundingSchemaFolder}/{schemaVersion}.json";

            try
            {
                bool versionExists = await CheckIfFundingSchemaVersionExists(blobName);

                if (!versionExists)
                {
                    return new NotFoundResult();
                }

                string schema = await GetFundingSchemaVersion(blobName);

                if (string.IsNullOrWhiteSpace(schema))
                {
                    _logger.Error($"Empty schema returned from blob storage for blob name '{blobName}'");
                    return new InternalServerErrorResult($"Failed to retrive blob contents for funding schema version '{schemaVersion}'");
                }

                return new OkObjectResult(schema);
            }
            catch(Exception ex)
            {
                _logger.Error(ex, $"Failed to fetch funding schema '{blobName}' from blob storage");

                return new InternalServerErrorResult($"Error occurred fetching funding schema verion '{schemaVersion}'");
            }
        }

        public async Task<IActionResult> SaveFundingSchema(string actionName, string controllerName, string schema)
        {
            Guard.IsNullOrWhiteSpace(actionName, nameof(actionName));
            Guard.IsNullOrWhiteSpace(controllerName, nameof(controllerName));

            //wont be null because of the above line
            if (schema.Trim() == string.Empty)
            {
                return new BadRequestObjectResult("Null or empty funding schema was provided.");
            }

            JSchema jSchema;
            try
            {
                jSchema = JSchema.Parse(schema); 
            }
            catch(Exception ex)
            {
                string errorMessage = "Failed to parse request body as a valid json schema.";

                _logger.Error(ex, errorMessage);

                return new BadRequestObjectResult(errorMessage);
            }

            string version = ExtractVersionFromSchema(jSchema);

            if(version == string.Empty)
            {
                return new BadRequestObjectResult("An invalid schema version was provided");
            }

            string blobName = $"{fundingSchemaFolder}/{version}.json";

            try
            {
                byte[] schemaFile = Encoding.UTF8.GetBytes(schema);

                await SaveFundingSchemaVersion(blobName, schemaFile);

                return new CreatedAtActionResult(actionName, controllerName, new { schemaVersion = version }, string.Empty);
            }
            catch(Exception ex)
            {
                _logger.Error(ex, $"Failed to save funding schema '{blobName}' to blob storage");

                return new InternalServerErrorResult("Error occurred uploading funding schema");
            }
        }

        private async Task<bool> CheckIfFundingSchemaVersionExists(string blobName)
        {
            try
            {
                return await _fundingSchemaRepositoryPolicy.ExecuteAsync(() => _fundingSchemaRepository.SchemaVersionExists(blobName));
            }
            catch(Exception ex)
            {
                _logger.Error($"Failed to check if funding schema version: '{blobName}' exists");

                throw new NonRetriableException($"Failed to check if funding schema version: '{blobName}' exists", ex);
            }
        }

        private async Task SaveFundingSchemaVersion(string blobName, byte[] schemaBytes)
        {
            try
            {
                await _fundingSchemaRepositoryPolicy.ExecuteAsync(() => _fundingSchemaRepository.SaveFundingSchemaVersion(blobName, schemaBytes));
            }
            catch (Exception ex)
            {
                throw new NonRetriableException($"Failed to save funding schema version: '{blobName}'", ex);
            }
        }

        private async Task<string> GetFundingSchemaVersion(string blobName)
        {
            try
            {
                return await _fundingSchemaRepositoryPolicy.ExecuteAsync(() => _fundingSchemaRepository.GetFundingSchemaVersion(blobName));
            }
            catch (Exception ex)
            {
                throw new NonRetriableException($"Failed to get funding schema version: '{blobName}' from blob storage", ex);
            }
        }

        private string ExtractVersionFromSchema(JSchema jSchema)
        {
            if (jSchema.ExtensionData.ContainsKey("version"))
            {
                JToken versionProperty = jSchema.ExtensionData["version"];

                if (versionProperty != null && !string.IsNullOrWhiteSpace(versionProperty.Value<string>()))
                {
                    if(decimal.TryParse(versionProperty.Value<string>(), out decimal version))
                    {
                        return version.ToString();
                    }
                }
            }

            return string.Empty;
        }
    }
}
