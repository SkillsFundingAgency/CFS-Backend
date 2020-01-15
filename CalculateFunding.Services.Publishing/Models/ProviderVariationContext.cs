using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;
using ApiProvider = CalculateFunding.Common.ApiClient.Providers.Models.Provider;
using PublishingProvider = CalculateFunding.Models.Publishing.Provider;

namespace CalculateFunding.Services.Publishing.Models
{
    public class ProviderVariationContext
    {
        private readonly Queue<IVariationChange> _variationChanges = new Queue<IVariationChange>();
        
        public string ProviderId { get; set; }

        /// <summary>
        /// Latest (current) core provider information which is being compared against the prior provider information
        /// </summary>
        public ApiProvider UpdatedProvider { get; set; }

        public ApiProvider SuccessorProvider { get; set; }

        public ProviderVariationResult Result { get; set; }

        public GeneratedProviderResult GeneratedProvider { get; set; }

        public PublishedProviderVersion PriorState { get; set; }

        public ICollection<string> ErrorMessages { get; } = new List<string>();

        public string FundingStreamId { get; set; }

        public string TemplateVersion { get; set; }

        public void QueueVariationChange(IVariationChange variationChange)
        {
            //the different concrete variation strategies can queue implementations 
            //of IVariationChange here to be run against this provider context
            //the changes are basically commands to run (moving funding about, creating new providers etc.)
            //the strategies instantiate the correct change and set its properties correctly so that it
            //can be run in order per provider
            
            _variationChanges.Enqueue(variationChange);    
        }

        public virtual async Task ApplyVariationChanges(IApplyProviderVariations variationsApplication)
        {
            while (_variationChanges.Count > 0)
            {
                IVariationChange variationChange = _variationChanges.Dequeue();

                await variationChange.Apply(variationsApplication);
            }
        }
    }
}
