using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Publishing.Interfaces;
using Serilog;

namespace CalculateFunding.Services.Publishing
{
    public class SpecificationFundingStatusService : ISpecificationFundingStatusService
    {
        private readonly ILogger _logger;
        private readonly ISpecificationService _specificationService;

        public SpecificationFundingStatusService(
            ILogger logger, 
            ISpecificationService specificationService)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(specificationService, nameof(specificationService));

            _logger = logger;
            _specificationService = specificationService;
        }

        public async Task<SpecificationFundingStatus> CheckChooseForFundingStatus(string specificationId)
        {
            Guard.IsNullOrWhiteSpace(specificationId, nameof(specificationId));

            SpecificationSummary specificationSummary = await _specificationService.GetSpecificationSummaryById(specificationId);

            if(specificationSummary == null)
            {
                string errorMessage = $"Failed to find specification with for specification Id '{specificationId}'";

                _logger.Error(errorMessage);

                throw new EntityNotFoundException(errorMessage, nameof(SpecificationSummary));
            }

            return await CheckChooseForFundingStatus(specificationSummary);
        }

        public async Task<SpecificationFundingStatus> CheckChooseForFundingStatus(SpecificationSummary specificationSummary)
        {
            Guard.ArgumentNotNull(specificationSummary, nameof(specificationSummary));

            if (specificationSummary.IsSelectedForFunding)
            {
                return SpecificationFundingStatus.AlreadyChosen;
            }

            string fundingPeriodId = specificationSummary.FundingPeriod.Id;

            IEnumerable<SpecificationSummary> specificationSummaries = await _specificationService.GetSpecificationsSelectedForFundingByPeriod(fundingPeriodId);

            if (specificationSummaries.IsNullOrEmpty())
            {
                return SpecificationFundingStatus.CanChoose;
            }

            HashSet<string> chosenFundingStreams = new HashSet<string>(specificationSummaries.SelectMany(m => m.FundingStreams.Select(fs => fs.Id)));

            IEnumerable<string> fundingStreamIds = specificationSummary.FundingStreams.Select(m => m.Id);

            if (chosenFundingStreams.Intersect(fundingStreamIds).Any())
            {
                return SpecificationFundingStatus.SharesAlreadyChoseFundingStream;
            }
            else
            {
                return SpecificationFundingStatus.CanChoose;
            }
        }
    }
}
