using AutoMapper;
using CalculateFunding.Common.Caching;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.FundingPolicy;
using CalculateFunding.Models.FundingPolicy.ViewModels;
using CalculateFunding.Models.Policy;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Interfaces.AzureStorage;
using CalculateFunding.Services.Policy.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Storage.Blob;
using Newtonsoft.Json;
using Newtonsoft.Json.Schema;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace CalculateFunding.Services.Policy
{
    public class FundingConfigurationService : IFundingConfigurationService, IHealthChecker
    {
        private readonly ILogger _logger;
        private readonly IMapper _mapper;
        private readonly IPolicyRepository _policyRepository;
        private readonly Polly.Policy _policyRepositoryPolicy;
        private readonly IValidator<FundingConfiguration> _fundingConfigurationValidator;

        public FundingConfigurationService(
            ILogger logger,
            IMapper mapper,
            IPolicyRepository policyRepository,
            IPolicyResilliencePolicies policyResilliencePolicies,
            IValidator<FundingConfiguration> fundingConfigurationValidator)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(mapper, nameof(mapper));
            Guard.ArgumentNotNull(policyRepository, nameof(policyRepository));
            Guard.ArgumentNotNull(policyResilliencePolicies, nameof(policyResilliencePolicies));
            Guard.ArgumentNotNull(fundingConfigurationValidator, nameof(fundingConfigurationValidator));

            _logger = logger;
            _mapper = mapper;
            _policyRepository = policyRepository;
            _policyRepositoryPolicy = policyResilliencePolicies.PolicyRepository;
            _fundingConfigurationValidator = fundingConfigurationValidator;
        }

        public async Task<ServiceHealth> IsHealthOk()
        {
            ServiceHealth policyRepoHealth = await ((IHealthChecker)_policyRepository).IsHealthOk();

            ServiceHealth health = new ServiceHealth()
            {
                Name = nameof(FundingConfigurationService)
            };
            health.Dependencies.AddRange(policyRepoHealth.Dependencies);
            
            return health;
        }

        public async Task<IActionResult> GetFundingConfiguration(string fundingStreamId, string fundingPeriodId)
        {
            if (string.IsNullOrWhiteSpace(fundingStreamId))
            {
                _logger.Error("No funding stream Id was provided to GetFundingConfiguration");

                return new BadRequestObjectResult("Null or empty funding stream Id provided");
            }

            if (string.IsNullOrWhiteSpace(fundingPeriodId))
            {
                _logger.Error("No funding period Id was provided to GetFundingConfiguration");

                return new BadRequestObjectResult("Null or empty funding period Id provided");
            }

            FundingConfiguration fundingConfiguration = await _policyRepositoryPolicy.ExecuteAsync(() => _policyRepository.GetFundingConfiguration(fundingStreamId, fundingPeriodId));

            if (fundingConfiguration == null)
            {
                _logger.Error($"No funding Configuration was found for funding stream id : {fundingStreamId} and funding period id : {fundingPeriodId}");

                return new NotFoundResult();
            }

            return new OkObjectResult(fundingConfiguration);
        }

        public async Task<IActionResult> SaveFundingConfiguration(string actionName, string controllerName, FundingConfigurationViewModel configurationViewModel, string fundingStreamId, string fundingPeriodId)
        {
            Guard.IsNullOrWhiteSpace(actionName, nameof(actionName));
            Guard.IsNullOrWhiteSpace(controllerName, nameof(controllerName));
            Guard.IsNullOrWhiteSpace(fundingStreamId, nameof(fundingStreamId));
            Guard.IsNullOrWhiteSpace(fundingPeriodId, nameof(fundingPeriodId));
            Guard.ArgumentNotNull(configurationViewModel, nameof(configurationViewModel));

            FundingConfiguration fundingConfiguration = _mapper.Map<FundingConfiguration>(configurationViewModel, opt =>
            {
                opt.Items["FundingStreamId"] = fundingStreamId;
                opt.Items["FundingPeriodId"] = fundingPeriodId;
                opt.Items["Id"] = Guid.NewGuid().ToString();
            });

            BadRequestObjectResult validationResult = (await _fundingConfigurationValidator.ValidateAsync(fundingConfiguration)).PopulateModelState();

            if (validationResult != null)
            {
                return validationResult;
            }

            try
            {
                HttpStatusCode result = await _policyRepositoryPolicy.ExecuteAsync(() => _policyRepository.SaveFundingConfiguration(fundingConfiguration));

                if (!result.IsSuccess())
                {
                    int statusCode = (int)result;

                    string errorMessage = $"Failed to save configuration file fzor funding stream id: {fundingStreamId} and period id: {fundingPeriodId} to cosmos db with status {statusCode}";

                    _logger.Error(errorMessage);

                    return new InternalServerErrorResult(errorMessage);
                }
            }
            catch (Exception exception)
            {
                string errorMessage = $"Exception occurred writing to configuration file for funding stream id: {fundingStreamId} and period id: {fundingPeriodId} to cosmos db";

                _logger.Error(exception, errorMessage);

                return new InternalServerErrorResult(errorMessage);
            }

            _logger.Information($"Successfully saved configuration file for funding stream id: {fundingStreamId} and period id: {fundingPeriodId} to cosmos db");

            return new CreatedAtActionResult(actionName, controllerName, new { fundingStreamId = fundingStreamId, fundingPeriodId = fundingPeriodId }, string.Empty);
        }
    }
}
