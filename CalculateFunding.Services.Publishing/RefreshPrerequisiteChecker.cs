using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Publishing.Interfaces;
using Serilog;
using System;

namespace CalculateFunding.Services.Publishing
{
    public class RefreshPrerequisiteChecker : IRefreshPrerequisiteChecker
    {
        private readonly ISpecificationFundingStatusService _specificationFundingStatusService;
        private readonly ISpecificationService _specificationService;
        private readonly ICalculationEngineRunningChecker _calculationEngineRunningChecker;
        private readonly ICalculationPrerequisiteCheckerService _calculationApprovalCheckerService;
        private readonly ILogger _logger;

        public RefreshPrerequisiteChecker(
            ISpecificationFundingStatusService specificationFundingStatusService,
            ISpecificationService specificationService,
            ICalculationEngineRunningChecker calculationEngineRunningChecker,
            ICalculationPrerequisiteCheckerService calculationApprovalCheckerService,
            ILogger logger)
        {
            Guard.ArgumentNotNull(specificationFundingStatusService, nameof(specificationFundingStatusService));
            Guard.ArgumentNotNull(specificationService, nameof(specificationService));
            Guard.ArgumentNotNull(calculationEngineRunningChecker, nameof(calculationEngineRunningChecker));
            Guard.ArgumentNotNull(calculationApprovalCheckerService, nameof(calculationApprovalCheckerService));
            Guard.ArgumentNotNull(logger, nameof(logger));

            _specificationFundingStatusService = specificationFundingStatusService;
            _specificationService = specificationService;
            _calculationEngineRunningChecker = calculationEngineRunningChecker;
            _calculationApprovalCheckerService = calculationApprovalCheckerService;
            _logger = logger;
        }

        public async Task<IEnumerable<string>> PerformPrerequisiteChecks(SpecificationSummary specification)
        {
            Guard.ArgumentNotNull(specification, nameof(specification));

            SpecificationFundingStatus specificationFundingStatus = await _specificationFundingStatusService.CheckChooseForFundingStatus(specification);

            if (specificationFundingStatus == SpecificationFundingStatus.SharesAlreadyChosenFundingStream)
            {
                string errorMessage = $"Specification with id: '{specification.Id} already shares chosen funding streams";

                _logger.Error(errorMessage);
                return new string[] { errorMessage };
            }

            if (specificationFundingStatus == SpecificationFundingStatus.CanChoose)
            {
                try
                {
                    await _specificationService.SelectSpecificationForFunding(specification.Id);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex.Message);
                    return new string[] { ex.Message };
                }
            }

            List<string> result = new List<string>();

            bool calculationEngineRunning = await _calculationEngineRunningChecker.IsCalculationEngineRunning(specification.Id, new string[] { JobConstants.DefinitionNames.CreateInstructAllocationJob });
            if (calculationEngineRunning)
            {
                result.Add("Calculation engine is still running");
            }

            IEnumerable<string> calculationPrereqValidationErrors = await _calculationApprovalCheckerService.VerifyCalculationPrerequisites(specification);
            if (calculationPrereqValidationErrors.AnyWithNullCheck())
            {
                result.AddRange(calculationPrereqValidationErrors);
            }

            _logger.Error(string.Join(Environment.NewLine, result));
            return result;
        }

    }
}
