using System;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Services.Publishing.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Polly;
using Serilog;

namespace CalculateFunding.Services.Publishing.Profiling
{
    public class FundingStreamPaymentDatesIngestion : IFundingStreamPaymentDatesIngestion
    {
        private readonly ILogger _logger;
        private readonly AsyncPolicy _resilience;
        private readonly IFundingStreamPaymentDatesRepository _fundingStreamPaymentDates;
        private readonly ICsvUtils _csvUtils;

        public FundingStreamPaymentDatesIngestion(IFundingStreamPaymentDatesRepository fundingStreamPaymentDates, 
            IPublishingResiliencePolicies resiliencePolicies, 
            ICsvUtils csvUtils,
            ILogger logger)
        {
            Guard.ArgumentNotNull(fundingStreamPaymentDates, nameof(fundingStreamPaymentDates));
            Guard.ArgumentNotNull(csvUtils, nameof(csvUtils));
            Guard.ArgumentNotNull(resiliencePolicies?.FundingStreamPaymentDatesRepository, nameof(resiliencePolicies.FundingStreamPaymentDatesRepository));
            Guard.ArgumentNotNull(logger, nameof(logger));
            
            _fundingStreamPaymentDates = fundingStreamPaymentDates;
            _resilience = resiliencePolicies.FundingStreamPaymentDatesRepository;
            _logger = logger;
            _csvUtils = csvUtils;
        }

        public async Task<IActionResult> IngestFundingStreamPaymentDates(string paymentDatesCsv, string fundingStreamId, string fundingPeriodId)
        {
            try
            {
                Guard.IsNullOrWhiteSpace(paymentDatesCsv, nameof(paymentDatesCsv));
                Guard.IsNullOrWhiteSpace(fundingStreamId, nameof(fundingStreamId));
                Guard.IsNullOrWhiteSpace(fundingPeriodId, nameof(fundingPeriodId));

                FundingStreamPaymentDate[] fundingStreamPaymentDates = _csvUtils.AsPocos<FundingStreamPaymentDate>(paymentDatesCsv)
                    .ToArray();
                
                _logger.Information($"Saving payment dates for {fundingStreamId}-{fundingPeriodId}");

                await _resilience.ExecuteAsync(() => _fundingStreamPaymentDates.SaveFundingStreamUpdatedDates(new FundingStreamPaymentDates
                {
                    FundingPeriodId = fundingPeriodId,
                    FundingStreamId = fundingStreamId,
                    PaymentDates = fundingStreamPaymentDates
                }));
                
                _logger.Information($"Saving payment dates for {fundingStreamId}-{fundingPeriodId}. {fundingStreamPaymentDates.Length} in total");

                return new OkResult();

            }
            catch(Exception ex)
            {
                _logger.Error(ex, "Unable to import paymentDatesCsv");

                throw;
            }
        }
    }
}