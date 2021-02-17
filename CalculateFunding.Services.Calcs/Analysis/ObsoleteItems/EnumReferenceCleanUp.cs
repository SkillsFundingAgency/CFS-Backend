using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.Models;
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
        private readonly AsyncPolicy _noOCCRetryResilience;

        public EnumReferenceCleanUp(ICalculationsRepository calculations,
            ICalcsResiliencePolicies resilience)
        {
            Guard.ArgumentNotNull(calculations, nameof(calculations));
            Guard.ArgumentNotNull(resilience?.CalculationsRepository, nameof(resilience.CalculationsRepository));
            Guard.ArgumentNotNull(resilience?.CalculationsRepositoryNoOCCRetry, nameof(resilience.CalculationsRepositoryNoOCCRetry));

            _calculations = calculations;
            _resilience = resilience.CalculationsRepository;
            _noOCCRetryResilience = resilience.CalculationsRepositoryNoOCCRetry;
        }

        public async Task ProcessCalculation(Calculation calculation)
        {
            Guard.ArgumentNotNull(calculation, nameof(calculation));

            await _resilience.ExecuteAsync(() => ReadObsoleteItemsAndProcessForCalculation(calculation));
        }

        private async Task ReadObsoleteItemsAndProcessForCalculation(Calculation calculation)
        {
            string sourceCode = calculation.Current.SourceCode;
            string calculationId = calculation.Id;

            IEnumerable<DocumentEntity<ObsoleteItem>> obsoleteEnumsInCalculation = GetObsoleteEnumDocumentsForCalculation(calculationId);

            foreach (DocumentEntity<ObsoleteItem> document in obsoleteEnumsInCalculation)
            {
                if (RemovedCalculationFromObsoleteEnum(document.Content, sourceCode, calculationId))
                {
                    await UpdateObsoleteEnum(document);
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

        private async Task UpdateObsoleteEnum(DocumentEntity<ObsoleteItem> obsoleteEnum)
        {
            if (obsoleteEnum.Content.IsEmpty)
            {
                await _noOCCRetryResilience.ExecuteAsync(() => _calculations.DeleteObsoleteItem(obsoleteEnum.Id, obsoleteEnum.ETag));
            }
            else
            {
                await _noOCCRetryResilience.ExecuteAsync(() => _calculations.UpdateObsoleteItem(obsoleteEnum.Content, obsoleteEnum.ETag));
            }
        }

        private IEnumerable<DocumentEntity<ObsoleteItem>> GetObsoleteEnumDocumentsForCalculation(string calculationId)
            => _calculations.GetObsoleteItemDocumentsForCalculation(calculationId, ObsoleteItemType.EnumValue);
    }
}