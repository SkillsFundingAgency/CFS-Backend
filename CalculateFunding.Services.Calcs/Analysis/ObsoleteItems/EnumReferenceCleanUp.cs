using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Calcs.ObsoleteItems;
using CalculateFunding.Services.Calcs.Interfaces;
using Polly;

namespace CalculateFunding.Services.Calcs.Analysis.ObsoleteItems
{
    public class EnumReferenceCleanUp : IEnumReferenceCleanUp
    {
        private readonly ICalculationsRepository _calculations;
        private readonly AsyncPolicy _resilience;

        public EnumReferenceCleanUp(ICalculationsRepository calculations,
            ICalcsResiliencePolicies resilience)
        {
            Guard.ArgumentNotNull(calculations, nameof(calculations));
            Guard.ArgumentNotNull(resilience?.CalculationsRepository, nameof(resilience.CalculationsRepository));

            _calculations = calculations;
            _resilience = resilience.CalculationsRepository;
        }

        public async Task ProcessCalculation(Calculation calculation)
        {
            Guard.ArgumentNotNull(calculation, nameof(calculation));

            string sourceCode = calculation.Current.SourceCode;
            string calculationId = calculation.Id;

            IEnumerable<ObsoleteItem> obsoleteEnumsInCalculation = await GetObsoleteEnumsForCalculation(calculationId);

            foreach (ObsoleteItem obsoleteEnum in obsoleteEnumsInCalculation)
            {
                if (RemovedCalculationFromObsoleteEnum(obsoleteEnum, sourceCode, calculationId))
                {
                    await UpdateObsoleteEnum(obsoleteEnum);
                }
            }
        }

        private bool RemovedCalculationFromObsoleteEnum(ObsoleteItem obsoleteEnum,
            string sourceCode,
            string calculationId)
        {
            if (!sourceCode.Contains(obsoleteEnum.CodeReference, StringComparison.InvariantCultureIgnoreCase))
            {
                return obsoleteEnum.TryRemoveCalculationId(calculationId);
            }

            return false;
        }

        //TODO; extend the base cosmos repo to support OCC with etags or timestamps
        private async Task UpdateObsoleteEnum(ObsoleteItem obsoleteEnum)
        {
            if (obsoleteEnum.IsEmpty)
            {
                await _resilience.ExecuteAsync(() => _calculations.DeleteObsoleteItem(obsoleteEnum.Id));
            }
            else
            {
                await _resilience.ExecuteAsync(() => _calculations.UpdateObsoleteItem(obsoleteEnum));
            }
        }

        private async Task<IEnumerable<ObsoleteItem>> GetObsoleteEnumsForCalculation(string calculationId)
            => await _resilience.ExecuteAsync(() => _calculations.GetObsoleteItemsForCalculation(calculationId, ObsoleteItemType.EnumValue));
    }
}