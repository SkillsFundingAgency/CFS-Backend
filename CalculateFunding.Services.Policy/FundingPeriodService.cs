using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.Caching;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Policy;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Policy.Interfaces;
using CalculateFunding.Services.Providers.Validators;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Serilog;
using FluentValidation;
using CalculateFunding.Services.Policy.Validators;

namespace CalculateFunding.Services.Policy
{
    public class FundingPeriodService : IFundingPeriodService, IHealthChecker
    {
        private readonly ILogger _logger;
        private readonly IPolicyRepository _policyRepository;
        private readonly ICacheProvider _cacheProvider;
        private readonly Polly.Policy _policyRepositoryPolicy;
        private readonly Polly.Policy _cacheProviderPolicy;      
        private readonly IValidator<FundingPeriodsJsonModel> _fundingPeriodJsonModelValidator;

        public FundingPeriodService(ILogger logger,
            ICacheProvider cacheProvider,
            IPolicyRepository policyRepository,
            IPolicyResiliencePolicies policyResiliencePolicies,
            IValidator<FundingPeriodsJsonModel> fundingPeriodJsonModelValidator)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(cacheProvider, nameof(cacheProvider));
            Guard.ArgumentNotNull(policyRepository, nameof(policyRepository));           
            Guard.ArgumentNotNull(policyResiliencePolicies?.PolicyRepository, nameof(policyResiliencePolicies.PolicyRepository));
            Guard.ArgumentNotNull(policyResiliencePolicies?.CacheProvider, nameof(policyResiliencePolicies.CacheProvider));
            Guard.ArgumentNotNull(fundingPeriodJsonModelValidator, nameof(fundingPeriodJsonModelValidator));

            _logger = logger;
            _cacheProvider = cacheProvider;
            _policyRepository = policyRepository;           
            _policyRepositoryPolicy = policyResiliencePolicies.PolicyRepository;
            _cacheProviderPolicy = policyResiliencePolicies.CacheProvider;
            _fundingPeriodJsonModelValidator = fundingPeriodJsonModelValidator;
        }

        public async Task<ServiceHealth> IsHealthOk()
        {
            ServiceHealth policyRepoHealth = await ((IHealthChecker)_policyRepository).IsHealthOk();
            (bool Ok, string Message) cacheRepoHealth = await _cacheProvider.IsHealthOk();

            ServiceHealth health = new ServiceHealth()
            {
                Name = nameof(FundingPeriodService)
            };
            health.Dependencies.AddRange(policyRepoHealth.Dependencies);
            health.Dependencies.Add(new DependencyHealth { HealthOk = cacheRepoHealth.Ok, DependencyName = cacheRepoHealth.GetType().GetFriendlyName(), Message = cacheRepoHealth.Message });

            return health;
        }

        public async Task<IActionResult> GetFundingPeriodById(string fundingPeriodId)
        {
            if (string.IsNullOrWhiteSpace(fundingPeriodId))
            {
                _logger.Error("No funding period id was provided to GetFundingPeriodById");

                return new BadRequestObjectResult("Null or empty funding period id provided");
            }

            FundingPeriod fundingPeriod = await _policyRepositoryPolicy.ExecuteAsync(() => _policyRepository.GetFundingPeriodById(fundingPeriodId));

            if (fundingPeriod == null)
            {
                _logger.Error($"No funding period was returned for funding period id: '{fundingPeriodId}'");

                return new NotFoundResult();
            }

            return new OkObjectResult(fundingPeriod);
        }

        public async Task<IActionResult> GetFundingPeriods()
        {
            IEnumerable<FundingPeriod> fundingPeriods = await _cacheProviderPolicy.ExecuteAsync(() => _cacheProvider.GetAsync<FundingPeriod[]>(CacheKeys.FundingPeriods));

            if (fundingPeriods.IsNullOrEmpty())
            {
                fundingPeriods = await _policyRepositoryPolicy.ExecuteAsync(() => _policyRepository.GetFundingPeriods());

                if (fundingPeriods.IsNullOrEmpty())
                {
                    _logger.Error("No funding periods were returned");

                    fundingPeriods = new FundingPeriod[0];
                }
                else
                {
                    await _cacheProviderPolicy.ExecuteAsync(() => _cacheProvider.SetAsync(CacheKeys.FundingPeriods, fundingPeriods.ToArraySafe()));
                }
            }

            return new OkObjectResult(fundingPeriods);
        }

        public async Task<IActionResult> SaveFundingPeriods(HttpRequest request)
        {
            string json = await request.GetRawBodyStringAsync();

            if (string.IsNullOrEmpty(json))
            {
                _logger.Error($"Null or empty json provided for file");
                return new BadRequestObjectResult($"Invalid json was provided for file");
            }

            FundingPeriodsJsonModel fundingPeriodsJsonModel = null;

            try
            {               
                fundingPeriodsJsonModel = JsonConvert.DeserializeObject<FundingPeriodsJsonModel>(json);

                BadRequestObjectResult validationResult = (await _fundingPeriodJsonModelValidator.ValidateAsync(fundingPeriodsJsonModel)).PopulateModelState();

                if (validationResult != null)
                {
                    return validationResult;
                }

            }
            catch (Exception exception)
            {
                _logger.Error(exception, $"Invalid json was provided for file");
                return new BadRequestObjectResult($"Invalid json was provided for file");
            }

            try
            {
                FundingPeriod[] fundingPeriods = fundingPeriodsJsonModel.FundingPeriods;

                if (!fundingPeriods.IsNullOrEmpty())
                {
                    await _policyRepositoryPolicy.ExecuteAsync(() => _policyRepository.SaveFundingPeriods(fundingPeriods));

                    await _cacheProviderPolicy.ExecuteAsync(() => _cacheProvider.RemoveAsync<FundingPeriod[]>(CacheKeys.FundingPeriods));

                    _logger.Information($"Upserted {fundingPeriods.Length} funding periods into cosomos");
                }
            }
            catch (Exception exception)
            {
                string errorMessage = $"Exception occurred writing json file to cosmos db";

                _logger.Error(exception, errorMessage);

                return new InternalServerErrorResult(errorMessage);
            }

            _logger.Information($"Successfully saved file to cosmos db");

            return new OkResult();
        }
    }
}
