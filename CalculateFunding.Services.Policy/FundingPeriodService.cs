using CalculateFunding.Common.Caching;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Policy;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Policy.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace CalculateFunding.Services.Policy
{
    public class FundingPeriodService : IFundingPeriodService, IHealthChecker
    {
        private readonly ILogger _logger;
        private readonly IPolicyRepository _policyRepository;
        private readonly ICacheProvider _cacheProvider;
        private readonly Polly.Policy _policyRepositoryPolicy;
        private readonly Polly.Policy _cacheProviderPolicy;

        public FundingPeriodService(ILogger logger,
            ICacheProvider cacheProvider,
            IPolicyRepository policyRepository,
            IPolicyResiliencePolicies policyResiliencePolicies)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(cacheProvider, nameof(cacheProvider));
            Guard.ArgumentNotNull(policyRepository, nameof(policyRepository));
            Guard.ArgumentNotNull(policyResiliencePolicies, nameof(policyResiliencePolicies));

            _logger = logger;
            _cacheProvider = cacheProvider;
            _policyRepository = policyRepository;
            _policyRepositoryPolicy = policyResiliencePolicies.PolicyRepository;
            _cacheProviderPolicy = policyResiliencePolicies.CacheProvider;
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

            Period fundingPeriod = await _policyRepositoryPolicy.ExecuteAsync(() => _policyRepository.GetFundingPeriodById(fundingPeriodId));

            if (fundingPeriod == null)
            {
                _logger.Error($"No funding period was returned for funding period id: '{fundingPeriodId}'");

                return new NotFoundResult();
            }

            return new OkObjectResult(fundingPeriod);
        }

        public async Task<IActionResult> GetFundingPeriods()
        {
            IEnumerable<Period> fundingPeriods = await _cacheProviderPolicy.ExecuteAsync(() => _cacheProvider.GetAsync<Period[]>(CacheKeys.FundingPeriods));

            if (fundingPeriods.IsNullOrEmpty())
            {
                fundingPeriods = await _policyRepositoryPolicy.ExecuteAsync(() => _policyRepository.GetFundingPeriods());

                if (fundingPeriods.IsNullOrEmpty())
                {
                    _logger.Error("No funding periods were returned");

                    fundingPeriods = new Period[0];
                }
                else
                {
                    await _cacheProviderPolicy.ExecuteAsync(() => _cacheProvider.SetAsync<Period[]>(CacheKeys.FundingPeriods, fundingPeriods.ToArraySafe()));
                }
            }

            return new OkObjectResult(fundingPeriods);
        }

        public async Task<IActionResult> SaveFundingPeriods(HttpRequest request)
        {
            string yaml = await request.GetRawBodyStringAsync();

            string yamlFilename = request.GetYamlFileNameFromRequest();

            if (string.IsNullOrEmpty(yaml))
            {
                _logger.Error($"Null or empty yaml provided for file: {yamlFilename}");
                return new BadRequestObjectResult($"Invalid yaml was provided for file: {yamlFilename}");
            }

            IDeserializer deserializer = new DeserializerBuilder()
                .WithNamingConvention(new CamelCaseNamingConvention())
                .Build();

            FundingPeriodsYamlModel fundingPeriodsYamlModel = null;

            try
            {
                fundingPeriodsYamlModel = deserializer.Deserialize<FundingPeriodsYamlModel>(yaml);
            }
            catch (Exception exception)
            {
                _logger.Error(exception, $"Invalid yaml was provided for file: {yamlFilename}");
                return new BadRequestObjectResult($"Invalid yaml was provided for file: {yamlFilename}");
            }

            try
            {
                if (!fundingPeriodsYamlModel.FundingPeriods.IsNullOrEmpty())
                {
                    await _policyRepositoryPolicy.ExecuteAsync(() => _policyRepository.SaveFundingPeriods(fundingPeriodsYamlModel.FundingPeriods));

                    await _cacheProviderPolicy.ExecuteAsync(() => _cacheProvider.SetAsync<Period[]>(CacheKeys.FundingPeriods, fundingPeriodsYamlModel.FundingPeriods));

                    _logger.Information($"Upserted {fundingPeriodsYamlModel.FundingPeriods.Length} funding periods into cosomos");
                }
            }
            catch (Exception exception)
            {
                string errorMessage = $"Exception occurred writing yaml file: {yamlFilename} to cosmos db";

                _logger.Error(exception, errorMessage);

                return new InternalServerErrorResult(errorMessage);
            }

            _logger.Information($"Successfully saved file: {yamlFilename} to cosmos db");

            return new OkResult();
        }
    }
}
