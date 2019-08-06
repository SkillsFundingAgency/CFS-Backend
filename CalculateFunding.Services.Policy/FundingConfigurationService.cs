using System;
using System.Net;
using System.Threading.Tasks;
using AutoMapper;
using CalculateFunding.Common.Caching;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.FundingPolicy;
using CalculateFunding.Models.FundingPolicy.ViewModels;
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
        private readonly Polly.Policy _policyRepositoryPolicy;
        private readonly Polly.Policy _cacheProviderPolicy;
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
            Guard.ArgumentNotNull(policyResiliencePolicies, nameof(policyResiliencePolicies));
            Guard.ArgumentNotNull(fundingConfigurationValidator, nameof(fundingConfigurationValidator));

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

            string cachKey = $"{CacheKeys.FundingConfig}{fundingStreamId}-{fundingPeriodId}";
            string configId = $"config-{fundingStreamId}-{fundingPeriodId}";

            FundingConfiguration fundingConfiguration = await _cacheProviderPolicy.ExecuteAsync(() => _cacheProvider.GetAsync<FundingConfiguration>(cachKey));

            if (fundingConfiguration == null)
            {
                fundingConfiguration = await _policyRepositoryPolicy.ExecuteAsync(() => _policyRepository.GetFundingConfiguration(configId));
                await _cacheProviderPolicy.ExecuteAsync(() => _cacheProvider.SetAsync<FundingConfiguration>(cachKey, fundingConfiguration));
            }

            if (fundingConfiguration == null)
            {
                _logger.Error($"No funding Configuration was found for funding stream id : {fundingStreamId} and funding period id : {fundingPeriodId}");

                return new NotFoundResult();
            }

            return new OkObjectResult(fundingConfiguration);
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

            string cacheKey = $"{CacheKeys.FundingConfig}{fundingStreamId}-{fundingPeriodId}";

            await _cacheProviderPolicy.ExecuteAsync(() => _cacheProvider.SetAsync(cacheKey, fundingConfiguration));

            _logger.Information($"Successfully saved configuration file for funding stream id: {fundingStreamId} and period id: {fundingPeriodId} to cosmos db");

            return new CreatedAtActionResult(actionName, controllerName, new {fundingStreamId, fundingPeriodId }, string.Empty);
        }
    }
}
