using CalculateFunding.Common.Caching;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Policy;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Policy.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;


namespace CalculateFunding.Services.Policy
{
    public class FundingStreamService : IFundingStreamService, IHealthChecker
    {
        private readonly ILogger _logger;
        private readonly IPolicyRepository _policyRepository;
        private readonly ICacheProvider _cacheProvider;
        private readonly Polly.AsyncPolicy _policyRepositoryPolicy;
        private readonly Polly.AsyncPolicy _cacheProviderPolicy;
        private readonly IValidator<FundingStreamSaveModel> _fundingStreamSaveModelValidator;

        public FundingStreamService(
            ILogger logger, 
            ICacheProvider cacheProvider, 
            IPolicyRepository policyRepository,
            IPolicyResiliencePolicies policyResiliencePolicies,
            IValidator<FundingStreamSaveModel> fundingStreamSaveModelValidator)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(cacheProvider, nameof(cacheProvider));
            Guard.ArgumentNotNull(policyRepository, nameof(policyRepository));
            Guard.ArgumentNotNull(policyResiliencePolicies?.PolicyRepository, nameof(policyResiliencePolicies.PolicyRepository));
            Guard.ArgumentNotNull(policyResiliencePolicies?.CacheProvider, nameof(policyResiliencePolicies.CacheProvider));
            Guard.ArgumentNotNull(fundingStreamSaveModelValidator, nameof(fundingStreamSaveModelValidator));

            _logger = logger;
            _cacheProvider = cacheProvider;
            _policyRepository = policyRepository;
            _policyRepositoryPolicy = policyResiliencePolicies.PolicyRepository;
            _cacheProviderPolicy = policyResiliencePolicies.CacheProvider;
            _fundingStreamSaveModelValidator = fundingStreamSaveModelValidator;
        }

        public async Task<ServiceHealth> IsHealthOk()
        {
            ServiceHealth policyRepoHealth = await ((IHealthChecker)_policyRepository).IsHealthOk();
            (bool Ok, string Message) cacheRepoHealth = await _cacheProvider.IsHealthOk();
           
            ServiceHealth health = new ServiceHealth()
            {
                Name = nameof(FundingStreamService)
            };
            health.Dependencies.AddRange(policyRepoHealth.Dependencies);
            health.Dependencies.Add(new DependencyHealth { HealthOk = cacheRepoHealth.Ok, DependencyName = cacheRepoHealth.GetType().GetFriendlyName(), Message = cacheRepoHealth.Message });

            return health;
        }

        public async Task<IActionResult> GetFundingStreams()
        {
            IEnumerable<FundingStream> fundingStreams = await _cacheProviderPolicy.ExecuteAsync(() => _cacheProvider.GetAsync<FundingStream[]>(CacheKeys.AllFundingStreams));

            if (fundingStreams.IsNullOrEmpty())
            {
                fundingStreams = await _policyRepositoryPolicy.ExecuteAsync(() => _policyRepository.GetFundingStreams());

                if (fundingStreams.IsNullOrEmpty())
                {
                    _logger.Error("No funding streams were returned");

                    fundingStreams = new FundingStream[0];
                }

                await _cacheProviderPolicy.ExecuteAsync(() => _cacheProvider.SetAsync<FundingStream[]>(CacheKeys.AllFundingStreams, fundingStreams.ToArray()));
            }

            return new OkObjectResult(fundingStreams);
        }

        public async Task<IActionResult> GetFundingStreamById(string fundingStreamId)
        {
            if (string.IsNullOrWhiteSpace(fundingStreamId))
            {
                _logger.Error("No funding stream Id was provided to GetFundingStreamById");

                return new BadRequestObjectResult("Null or empty funding stream Id provided");
            }

            FundingStream fundingStream = await _policyRepositoryPolicy.ExecuteAsync(() => _policyRepository.GetFundingStreamById(fundingStreamId));

            if (fundingStream == null)
            {
                _logger.Error($"No funding stream was found for funding stream id : {fundingStreamId}");

                return new NotFoundResult();
            }

            return new OkObjectResult(fundingStream);
        }

        public async Task<IActionResult> SaveFundingStream(FundingStreamSaveModel fundingStreamSaveModel)
        {
            if (fundingStreamSaveModel == null)
            {
                _logger.Error($"Null or empty json provided for file");
                return new BadRequestObjectResult($"Invalid json was provided for file");
            }

            try
            {
                BadRequestObjectResult validationResult = (await _fundingStreamSaveModelValidator.ValidateAsync(fundingStreamSaveModel)).PopulateModelState();

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
                FundingStream fundingStream = new FundingStream()
                {
                    Id = fundingStreamSaveModel.Id,
                    Name = fundingStreamSaveModel.Name,
                    ShortName = fundingStreamSaveModel.ShortName
                };

                if (fundingStream != null)
                {
                    HttpStatusCode result = await _policyRepositoryPolicy.ExecuteAsync(() => _policyRepository.SaveFundingStream(fundingStream));

                    if (!result.IsSuccess())
                    {
                        int statusCode = (int)result;

                        _logger.Error($"Failed to save to cosmos db with status {statusCode}");

                        return new StatusCodeResult(statusCode);
                    }
                }
            }
            catch (Exception exception)
            {
                string errorMessage = $"Exception occurred writing to json file to cosmos db";

                _logger.Error(exception, errorMessage);

                return new InternalServerErrorResult(errorMessage);
            }

            _logger.Information($"Successfully saved file to cosmos db");

            bool keyExists = await _cacheProviderPolicy.ExecuteAsync(() => _cacheProvider.KeyExists<FundingStream[]>(CacheKeys.AllFundingStreams));

            if (keyExists)
            {
                await _cacheProviderPolicy.ExecuteAsync(() => _cacheProvider.KeyDeleteAsync<FundingStream[]>(CacheKeys.AllFundingStreams));
            }

            return new OkResult();
        }
    }
}
