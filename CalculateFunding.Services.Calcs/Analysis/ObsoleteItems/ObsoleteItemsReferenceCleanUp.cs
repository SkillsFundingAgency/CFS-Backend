using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Calcs.ObsoleteItems;
using CalculateFunding.Services.Calcs.Interfaces;
using Polly;

namespace CalculateFunding.Services.Calcs.Analysis.ObsoleteItems
{
    public abstract class ObsoleteItemsReferenceCleanUp : IObsoleteReferenceCleanUp
    {
        private readonly ICalculationsRepository _calculations;
        private readonly AsyncPolicy _resilience;
        private readonly AsyncPolicy _noOccRetryResilience;
        private readonly ObsoleteItemType _obsoleteItemType;

        protected ObsoleteItemsReferenceCleanUp(ICalculationsRepository calculations,
            ICalcsResiliencePolicies resilience,
            ObsoleteItemType obsoleteItemType)
        {
            Guard.ArgumentNotNull(calculations, nameof(calculations));
            Guard.ArgumentNotNull(resilience?.CalculationsRepository, nameof(resilience.CalculationsRepository));
            Guard.ArgumentNotNull(resilience?.CalculationsRepositoryNoOCCRetry, nameof(resilience.CalculationsRepositoryNoOCCRetry));

            _calculations = calculations;
            _obsoleteItemType = obsoleteItemType;
            _resilience = resilience.CalculationsRepository;
            _noOccRetryResilience = resilience.CalculationsRepositoryNoOCCRetry;
        }

        protected async Task UpdateObsoleteEnum(DocumentEntity<ObsoleteItem> obsoleteEnum)
        {
            if (obsoleteEnum.Content.IsEmpty)
            {
                await _noOccRetryResilience.ExecuteAsync(() => _calculations.DeleteObsoleteItem(obsoleteEnum.Id, obsoleteEnum.ETag));
            }
            else
            {
                await _noOccRetryResilience.ExecuteAsync(() => _calculations.UpdateObsoleteItem(obsoleteEnum.Content, obsoleteEnum.ETag));
            }
        }

        protected IEnumerable<DocumentEntity<ObsoleteItem>> GetObsoleteDocumentsForCalculation(string calculationId)
            => _calculations.GetObsoleteItemDocumentsForCalculation(calculationId, _obsoleteItemType);

        protected bool RemovedCalculationFromObsoleteEnum(ObsoleteItem obsoleteEnum,
            string sourceCode,
            string calculationId)
        {
            if (!sourceCode.Contains(obsoleteEnum.CodeReference, StringComparison.InvariantCultureIgnoreCase))
            {
                return obsoleteEnum.TryRemoveCalculationId(calculationId);
            }

            return false;
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

            IEnumerable<DocumentEntity<ObsoleteItem>> obsoleteEnumsInCalculation = GetObsoleteDocumentsForCalculation(calculationId);

            foreach (DocumentEntity<ObsoleteItem> document in obsoleteEnumsInCalculation)
            {
                if (RemovedCalculationFromObsoleteEnum(document.Content, sourceCode, calculationId))
                {
                    await UpdateObsoleteEnum(document);
                }
            }
        }
    }
}