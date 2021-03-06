﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using AutoMapper;
using CalculateFunding.Common.Caching;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Policy.FundingPolicy;
using CalculateFunding.Models.Policy.FundingPolicy.ViewModels;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Policy.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace CalculateFunding.Services.Policy
{
    public class FundingConfigurationService : IFundingConfigurationService, IHealthChecker
    {
        private readonly ILogger _logger;
        private readonly ICacheProvider _cacheProvider;
        private readonly IMapper _mapper;
        private readonly IPolicyRepository _policyRepository;
        private readonly Polly.AsyncPolicy _policyRepositoryPolicy;
        private readonly Polly.AsyncPolicy _cacheProviderPolicy;
        private readonly IValidator<FundingConfiguration> _fundingConfigurationValidator;

        public FundingConfigurationService(
            ILogger logger,
            ICacheProvider cacheProvider,
            IMapper mapper,
            IPolicyRepository policyRepository,
            IPolicyResiliencePolicies policyResiliencePolicies,
            IValidator<FundingConfiguration> fundingConfigurationValidator)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(cacheProvider, nameof(cacheProvider));
            Guard.ArgumentNotNull(mapper, nameof(mapper));
            Guard.ArgumentNotNull(policyRepository, nameof(policyRepository));
            Guard.ArgumentNotNull(fundingConfigurationValidator, nameof(fundingConfigurationValidator));
            Guard.ArgumentNotNull(policyResiliencePolicies?.CacheProvider, nameof(policyResiliencePolicies.CacheProvider));
            Guard.ArgumentNotNull(policyResiliencePolicies?.PolicyRepository, nameof(policyResiliencePolicies.PolicyRepository));

            _logger = logger;
            _cacheProvider = cacheProvider;
            _cacheProviderPolicy = policyResiliencePolicies.CacheProvider;
            _mapper = mapper;
            _policyRepository = policyRepository;
            _policyRepositoryPolicy = policyResiliencePolicies.PolicyRepository;
            _fundingConfigurationValidator = fundingConfigurationValidator;
        }

        public async Task<ServiceHealth> IsHealthOk()
        {
            ServiceHealth policyRepoHealth = await ((IHealthChecker)_policyRepository).IsHealthOk();
            (bool Ok, string Message) cacheRepoHealth = await _cacheProvider.IsHealthOk();

            ServiceHealth health = new ServiceHealth()
            {
                Name = nameof(FundingConfigurationService)
            };
            health.Dependencies.AddRange(policyRepoHealth.Dependencies);
            health.Dependencies.Add(new DependencyHealth { HealthOk = cacheRepoHealth.Ok, DependencyName = cacheRepoHealth.GetType().GetFriendlyName(), Message = cacheRepoHealth.Message });

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

            string cacheKey = $"{CacheKeys.FundingConfig}{fundingStreamId}-{fundingPeriodId}";
            string configId = $"config-{fundingStreamId}-{fundingPeriodId}";

            FundingConfiguration fundingConfiguration = await _cacheProviderPolicy.ExecuteAsync(() => _cacheProvider.GetAsync<FundingConfiguration>(cacheKey));

            if (fundingConfiguration == null)
            {
                fundingConfiguration = await _policyRepositoryPolicy.ExecuteAsync(() => _policyRepository.GetFundingConfiguration(configId));
                await _cacheProviderPolicy.ExecuteAsync(() => _cacheProvider.SetAsync(cacheKey, fundingConfiguration));
            }

            if (fundingConfiguration == null)
            {
                _logger.Error($"No funding Configuration was found for funding stream id : {fundingStreamId} and funding period id : {fundingPeriodId}");

                return new NotFoundResult();
            }

            return new OkObjectResult(fundingConfiguration);
        }

        public async Task<IActionResult> GetFundingConfigurationsByFundingStreamId(string fundingStreamId)
        {
            if (string.IsNullOrWhiteSpace(fundingStreamId))
            {
                _logger.Error("No funding stream Id was provided to GetFundingConfigurationsByFundingStreamId");

                return new BadRequestObjectResult("Null or empty funding stream Id provided");
            }

            string cacheKey = $"{CacheKeys.FundingConfig}{fundingStreamId}";
           
            IEnumerable<FundingConfiguration> fundingConfigurations = await _cacheProviderPolicy.ExecuteAsync(() => _cacheProvider.GetAsync<List<FundingConfiguration>>(cacheKey));

            if (!fundingConfigurations.IsNullOrEmpty())
            {
                return new OkObjectResult(fundingConfigurations);
            }

            fundingConfigurations = await _policyRepositoryPolicy.ExecuteAsync(() => _policyRepository.GetFundingConfigurationsByFundingStreamId(fundingStreamId));

            if (fundingConfigurations.IsNullOrEmpty())
            {
                _logger.Error($"No funding Configurations were found for funding stream id : {fundingStreamId}");

                return new NotFoundResult();
            }

            await _cacheProviderPolicy.ExecuteAsync(() => _cacheProvider.SetAsync(cacheKey, fundingConfigurations.ToList()));

            return new OkObjectResult(fundingConfigurations);
        }

        public async Task<IActionResult> SaveFundingConfiguration(string actionName, 
            string controllerName, 
            FundingConfigurationViewModel configurationViewModel, 
            string fundingStreamId, 
            string fundingPeriodId)
        {
            Guard.IsNullOrWhiteSpace(actionName, nameof(actionName));
            Guard.IsNullOrWhiteSpace(controllerName, nameof(controllerName));
            Guard.IsNullOrWhiteSpace(fundingStreamId, nameof(fundingStreamId));
            Guard.IsNullOrWhiteSpace(fundingPeriodId, nameof(fundingPeriodId));
            Guard.ArgumentNotNull(configurationViewModel, nameof(configurationViewModel));

            FundingConfiguration fundingConfiguration = _mapper.Map<FundingConfiguration>(configurationViewModel, opt =>
            {
                opt.Items[nameof(FundingConfiguration.FundingStreamId)] = fundingStreamId;
                opt.Items[nameof(FundingConfiguration.FundingPeriodId)] = fundingPeriodId;
            });

            BadRequestObjectResult validationResult = (await _fundingConfigurationValidator.ValidateAsync(fundingConfiguration)).PopulateModelState();

            if (validationResult != null)
            {
                return validationResult;
            }
            
            //TODO; add validation on existence of template (what is a template exactly) when supplied as the default template id (funding template I'm guessing)

            try
            {
                HttpStatusCode result = await _policyRepositoryPolicy.ExecuteAsync(() => _policyRepository.SaveFundingConfiguration(fundingConfiguration));

                if (!result.IsSuccess())
                {
                    int statusCode = (int)result;

                    string errorMessage = $"Failed to save configuration file for funding stream id: {fundingStreamId} and period id: {fundingPeriodId} to cosmos db with status {statusCode}";

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

            string fundingPeriodFundingConfigurationCacheKey = $"{CacheKeys.FundingConfig}{fundingStreamId}-{fundingPeriodId}";
            await _cacheProviderPolicy.ExecuteAsync(() => _cacheProvider.SetAsync(fundingPeriodFundingConfigurationCacheKey, fundingConfiguration));

            string fundingStreamFundingConfigurationCacheKey = $"{CacheKeys.FundingConfig}{fundingStreamId}";
            await _cacheProviderPolicy.ExecuteAsync(() => _cacheProvider.RemoveAsync<List<FundingConfiguration>>(fundingStreamFundingConfigurationCacheKey));

            _logger.Information($"Successfully saved configuration file for funding stream id: {fundingStreamId} and period id: {fundingPeriodId} to cosmos db");

            return new CreatedAtActionResult(actionName, controllerName, new {fundingStreamId, fundingPeriodId }, string.Empty);
        }
    }
}
