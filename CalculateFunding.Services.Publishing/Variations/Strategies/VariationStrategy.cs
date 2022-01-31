using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.Variations.Strategies
{
    public abstract class VariationStrategy : IVariationStrategy
    {
        public const string Closed = "Closed";
        public const string Opened = "Open";
        public const string AcademyConverter = "Academy Converter";

        public abstract string Name { get; }

        public async Task<bool> Process(ProviderVariationContext providerVariationContext, IEnumerable<string> fundingLineCodes)
        {
            Guard.ArgumentNotNull(providerVariationContext, nameof(providerVariationContext));

            if (await Determine(providerVariationContext, fundingLineCodes))
            {
                Register(providerVariationContext);
                return await Execute(providerVariationContext);
            }

            return false;
        }

        protected abstract Task<bool> Determine(ProviderVariationContext providerVariationContext, IEnumerable<string> fundingLineCodes);

        private void Register(ProviderVariationContext providerVariationContext)
        {
            providerVariationContext.ApplicableVariations.Add(Name);
        }

        protected void RecordError(ProviderVariationContext context, string error)
        {
            context.LogError($"Unable to apply strategy '{Name}' on provider id '{context.ProviderId}'{error}");
            context.RecordError(context.ProviderId,
                    $"Unable to apply strategy '{Name}'",
                    error);

        }

        protected abstract Task<bool> Execute(ProviderVariationContext providerVariationContext);
    }
}