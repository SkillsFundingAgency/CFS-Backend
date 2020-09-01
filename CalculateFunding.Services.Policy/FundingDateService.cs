using AutoMapper;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Policy.FundingPolicy;
using CalculateFunding.Models.Policy.FundingPolicy.ViewModels;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Policy.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using System;
using System.Net;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Policy
{
    public class FundingDateService : IFundingDateService, IHealthChecker
    {
        private readonly IPolicyRepository _policyRepository;
        private readonly ILogger _logger;
        private readonly Polly.AsyncPolicy _policyRepositoryPolicy;
        private readonly IValidator<FundingDate> _fundingDateValidator;
        private readonly IMapper _mapper;

        public FundingDateService(
            IPolicyRepository policyRepository,
            IPolicyResiliencePolicies policyResiliencePolicies,
            ILogger logger,
            IValidator<FundingDate> fundingDateValidator,
            IMapper mapper)
        {
            Guard.ArgumentNotNull(policyRepository, nameof(policyRepository));
            Guard.ArgumentNotNull(policyResiliencePolicies, nameof(policyResiliencePolicies));
            Guard.ArgumentNotNull(policyResiliencePolicies?.PolicyRepository, nameof(policyResiliencePolicies.PolicyRepository));
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(fundingDateValidator, nameof(fundingDateValidator));
            Guard.ArgumentNotNull(mapper, nameof(mapper));

            _policyRepository = policyRepository;
            _logger = logger;
            _policyRepositoryPolicy = policyResiliencePolicies.PolicyRepository;
            _fundingDateValidator = fundingDateValidator;
            _mapper = mapper;
        }

        public async Task<ServiceHealth> IsHealthOk()
        {
            ServiceHealth policyRepoHealth = await ((IHealthChecker)_policyRepository).IsHealthOk();

            ServiceHealth health = new ServiceHealth()
            {
                Name = nameof(FundingDateService)
            };
            health.Dependencies.AddRange(policyRepoHealth.Dependencies);

            return health;
        }

        public async Task<IActionResult> GetFundingDate(
            string fundingStreamId, 
            string fundingPeriodId, 
            string fundingLineId)
        {
            if (string.IsNullOrWhiteSpace(fundingStreamId))
            {
                _logger.Error("No funding stream Id was provided to GetFundingDate");

                return new BadRequestObjectResult("Null or empty funding stream Id provided");
            }

            if (string.IsNullOrWhiteSpace(fundingPeriodId))
            {
                _logger.Error("No funding period Id was provided to GetFundingDate");

                return new BadRequestObjectResult("Null or empty funding period Id provided");
            }

            if (string.IsNullOrWhiteSpace(fundingLineId))
            {
                _logger.Error("No funding line Id was provided to GetFundingDate");

                return new BadRequestObjectResult("Null or empty funding line Id provided");
            }

            string fundingDateId = $"fundingdate-{fundingStreamId}-{fundingPeriodId}-{fundingLineId}";

            FundingDate fundingDate = null;

            try
            {
                fundingDate = await _policyRepositoryPolicy.ExecuteAsync(() => _policyRepository.GetFundingDate(fundingDateId));
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"No funding Dates were found for funding stream id : {fundingStreamId}");
            }

            if (fundingDate == null)
            {
                _logger.Error($"No funding Dates were found for funding stream id : {fundingStreamId}");

                return new NotFoundResult();
            }

            return new OkObjectResult(fundingDate);
        }

        public async Task<IActionResult> SaveFundingDate(
            string actionName,
            string controllerName,
            string fundingStreamId, 
            string fundingPeriodId, 
            string fundingLineId,
            FundingDateViewModel fundingDateViewModel)
        {
            if (string.IsNullOrWhiteSpace(fundingStreamId))
            {
                _logger.Error("No funding stream Id was provided to SaveFundingDate");

                return new BadRequestObjectResult("Null or empty funding stream Id provided");
            }

            if (string.IsNullOrWhiteSpace(fundingPeriodId))
            {
                _logger.Error("No funding period Id was provided to SaveFundingDate");

                return new BadRequestObjectResult("Null or empty funding period Id provided");
            }

            if (string.IsNullOrWhiteSpace(fundingLineId))
            {
                _logger.Error("No funding line Id was provided to SaveFundingDate");

                return new BadRequestObjectResult("Null or empty funding line Id provided");
            }

            if (fundingDateViewModel == null)
            {
                _logger.Error("No funding date view model was provided to SaveFundingDate");

                return new BadRequestObjectResult("Null or empty funding date view model provided");
            }

            FundingDate fundingDate = _mapper.Map<FundingDate>(
                fundingDateViewModel, 
                opt =>
            {
                opt.Items[nameof(FundingDate.FundingStreamId)] = fundingStreamId;
                opt.Items[nameof(FundingDate.FundingPeriodId)] = fundingPeriodId;
                opt.Items[nameof(FundingDate.FundingLineId)] = fundingLineId;
            });

            BadRequestObjectResult validationResult = 
                (await _fundingDateValidator.ValidateAsync(fundingDate)).PopulateModelState();

            if (validationResult != null)
            {
                return validationResult;
            }

            try
            {
                HttpStatusCode result = await _policyRepositoryPolicy.ExecuteAsync(
                    () => _policyRepository.SaveFundingDate(fundingDate));

                if (!result.IsSuccess())
                {
                    int statusCode = (int)result;

                    string errorMessage = $"Failed to save funding date for funding stream id: {fundingStreamId} and period id: {fundingPeriodId} and funding line id: {fundingLineId} to cosmos db with status {statusCode}";

                    _logger.Error(errorMessage);

                    return new InternalServerErrorResult(errorMessage);
                }
            }
            catch (Exception exception)
            {
                string errorMessage = $"Exception occurred writing to funding date for funding stream id: {fundingStreamId} and period id: {fundingPeriodId} and funding line id: {fundingLineId} to cosmos db";

                _logger.Error(exception, errorMessage);

                return new InternalServerErrorResult(errorMessage);
            }

            _logger.Information($"Successfully saved funding date for funding stream id: {fundingStreamId} and period id: {fundingPeriodId} and funding line id: {fundingLineId} to cosmos db");

            return new CreatedAtActionResult(
                actionName, 
                controllerName, 
                new { fundingStreamId, fundingPeriodId, fundingLineId }, string.Empty);
        }
    }
}
