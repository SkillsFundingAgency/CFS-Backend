using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Publishing.Interfaces;

namespace CalculateFunding.Services.Publishing
{
    public class RefreshPrerequisiteChecker : IRefreshPrerequisiteChecker
    {
        private readonly ISpecificationFundingStatusService _specificationFundingStatusService;
        private readonly ISpecificationService _specificationService;
        private readonly ICalculationEngineRunningChecker _calculationEngineRunningChecker;
        private readonly ICalculationPrerequisiteCheckerService _calculationApprovalCheckerService;

        public RefreshPrerequisiteChecker(
            ISpecificationFundingStatusService specificationFundingStatusService,
            ISpecificationService specificationService,
            ICalculationEngineRunningChecker calculationEngineRunningChecker,
            ICalculationPrerequisiteCheckerService calculationApprovalCheckerService)
        {
            Guard.ArgumentNotNull(specificationFundingStatusService, nameof(specificationFundingStatusService));
            Guard.ArgumentNotNull(specificationService, nameof(specificationService));
            Guard.ArgumentNotNull(calculationEngineRunningChecker, nameof(calculationEngineRunningChecker));
            Guard.ArgumentNotNull(calculationApprovalCheckerService, nameof(calculationApprovalCheckerService));

            _specificationFundingStatusService = specificationFundingStatusService;
            _specificationService = specificationService;
            _calculationEngineRunningChecker = calculationEngineRunningChecker;
            _calculationApprovalCheckerService = calculationApprovalCheckerService;
        }

        public async Task<IEnumerable<string>> PerformPrerequisiteChecks(SpecificationSummary specification)
        {
            Guard.ArgumentNotNull(specification, nameof(specification));

            SpecificationFundingStatus specificationFundingStatus = await _specificationFundingStatusService.CheckChooseForFundingStatus(specification);

            if (specificationFundingStatus == SpecificationFundingStatus.SharesAlreadyChoseFundingStream)
            {
                string errorMessage = $"Specification with id: '{specification.Id} already shares chosen funding streams";

                return new string[] { errorMessage };
            }

            if (specificationFundingStatus == SpecificationFundingStatus.CanChoose)
            {
                await _specificationService.SelectSpecificationForFunding(specification.Id);
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

            return result;
        }
    }
}
