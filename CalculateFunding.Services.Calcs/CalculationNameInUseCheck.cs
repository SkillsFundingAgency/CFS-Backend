using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Specs;
using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Services.CodeGeneration.VisualBasic;
using Polly;

namespace CalculateFunding.Services.Calcs
{
    public class CalculationNameInUseCheck : ICalculationNameInUseCheck
    {
        private readonly ICalculationsRepository _calculationsRepository;
        private readonly ISpecificationRepository _specsRepository;
        private readonly Policy _specificationsRepositoryPolicy;
        private readonly Policy _calculationRepositoryPolicy;

        public CalculationNameInUseCheck(ICalculationsRepository calculationsRepository, 
            ISpecificationRepository specsRepository,
            ICalcsResiliencePolicies resiliencePolicies)
        {
            Guard.ArgumentNotNull(calculationsRepository, nameof(calculationsRepository));
            Guard.ArgumentNotNull(specsRepository, nameof(specsRepository));
            Guard.ArgumentNotNull(resiliencePolicies?.CalculationsRepository, nameof(resiliencePolicies.CalculationsRepository));
            Guard.ArgumentNotNull(resiliencePolicies?.SpecificationsRepositoryPolicy, nameof(resiliencePolicies.SpecificationsRepositoryPolicy));
            
            _calculationsRepository = calculationsRepository;
            _specsRepository = specsRepository;
            _specificationsRepositoryPolicy = resiliencePolicies.SpecificationsRepositoryPolicy;
            _calculationRepositoryPolicy = resiliencePolicies.CalculationsRepository;
        }

        public async Task<bool?> IsCalculationNameInUse(string specificationId, string calculationName, string existingCalculationId)
        {
            Guard.IsNullOrWhiteSpace(specificationId, nameof(specificationId));
            Guard.IsNullOrWhiteSpace(calculationName, nameof(calculationName));

            SpecificationSummary specification = await _specificationsRepositoryPolicy.ExecuteAsync(() => _specsRepository.GetSpecificationSummaryById(specificationId));

            if (specification == null)
            {
                return null;
            }

            IEnumerable<Calculation> existingCalculations = await _calculationRepositoryPolicy.ExecuteAsync(() => _calculationsRepository.GetCalculationsBySpecificationId(specificationId));

            if (!existingCalculations.IsNullOrEmpty())
            {
                string calcSourceName = VisualBasicTypeGenerator.GenerateIdentifier(calculationName);

                foreach (Calculation calculation in existingCalculations)
                {
                    if (calculation.Id != existingCalculationId && string.Compare(calculation.Current.SourceCodeName, calcSourceName, true) == 0)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}