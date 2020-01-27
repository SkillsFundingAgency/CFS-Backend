using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Publishing.Interfaces;
using Microsoft.Azure.Amqp.Framing;
using ApiProvider = CalculateFunding.Common.ApiClient.Providers.Models.Provider;
using PublishingProvider = CalculateFunding.Models.Publishing.Provider;

namespace CalculateFunding.Services.Publishing.Models
{
    public class ProviderVariationContext
    {
        private readonly Queue<IVariationChange> _variationChanges = new Queue<IVariationChange>();

        public string ProviderId => RefreshState?.ProviderId;

        /// <summary>
        /// Latest (current) core provider information which is being compared against the prior provider information
        /// </summary>
        public ApiProvider UpdatedProvider { get; set; }

        public ApiProvider SuccessorProvider { get; set; }

        public ProviderVariationResult Result { get; set; }

        public GeneratedProviderResult GeneratedProvider { get; set; }

        /// <summary>
        /// The calling code uses side effects on the PublishedProvider.Current instance
        /// to map changes made during refresh into a new published provider downstream
        /// so changes in the variations should alter the state of this PublishedProviderVersion
        /// </summary>
        public PublishedProviderVersion RefreshState => PublishedProvider?.Current;

        /// <summary>
        /// The variation detection should compare the updated provider with the last published details
        /// here using the latest released publishedproviderversion
        /// </summary>
        public PublishedProviderVersion ReleasedState => PublishedProvider?.Released;
        
        public PublishedProvider PublishedProvider { get; set; }

        public ICollection<string> ErrorMessages { get; } = new List<string>();

        public string FundingStreamId { get; set; }

        public string TemplateVersion { get; set; }

        public void RecordErrors(params string[] errors)
        {
            ErrorMessages.AddRange(errors);
        }

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

        public IEnumerable<IVariationChange> QueuedChanges => _variationChanges.ToArray();
    }
}
