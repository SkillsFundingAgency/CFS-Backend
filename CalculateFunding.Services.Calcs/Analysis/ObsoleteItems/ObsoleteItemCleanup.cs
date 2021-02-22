using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Services.Core.Helpers;

namespace CalculateFunding.Services.Calcs.Analysis.ObsoleteItems
{
    public class ObsoleteItemCleanup : IObsoleteItemCleanup
    {
        private readonly IObsoleteReferenceCleanUp[] _obsoleteItemReferenceCleanUpSteps;
        
        public ObsoleteItemCleanup(IEnumerable<IObsoleteReferenceCleanUp> obsoleteItemReferenceCleanUpSteps)
        {
            Guard.ArgumentNotNull(obsoleteItemReferenceCleanUpSteps, nameof(obsoleteItemReferenceCleanUpSteps));
            
            _obsoleteItemReferenceCleanUpSteps = obsoleteItemReferenceCleanUpSteps.ToArray();
        }

        public async Task ProcessCalculation(Calculation calculation)
        {
            Guard.ArgumentNotNull(calculation, nameof(calculation));
            
            Task[] cleanUpTasks = _obsoleteItemReferenceCleanUpSteps.Select(_ => _.ProcessCalculation(calculation))
                .ToArray();

            await TaskHelper.WhenAllAndThrow(cleanUpTasks);
        }
    }
}