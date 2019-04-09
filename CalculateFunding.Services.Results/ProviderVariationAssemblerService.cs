using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Providers;
using CalculateFunding.Models.Results;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Providers.Interfaces;
using CalculateFunding.Services.Results.Interfaces;

namespace CalculateFunding.Services.Results
{
    public class ProviderVariationAssemblerService : IProviderVariationAssemblerService
    {
        private const string ProviderStatusClosed = "Closed";

        private readonly IProviderService _providerService;

        public ProviderVariationAssemblerService(IProviderService providerService)
        {
            Guard.ArgumentNotNull(providerService, nameof(providerService));

            _providerService = providerService;
        }

        public async Task<IEnumerable<ProviderChangeItem>> AssembleProviderVariationItems(IEnumerable<ProviderResult> providerResults, IEnumerable<PublishedProviderResultExisting> existingPublishedProviderResults, string specificationId)
        {
            List<ProviderChangeItem> changeItems = new List<ProviderChangeItem>();

            IEnumerable<ProviderSummary> coreProviderData = await _providerService.FetchCoreProviderData();

            if (coreProviderData.IsNullOrEmpty())
            {
                throw new NonRetriableException("Failed to retrieve core provider data");
            }

            foreach (ProviderResult providerResult in providerResults)
            {
                PublishedProviderResultExisting existingResult = existingPublishedProviderResults.FirstOrDefault(r => r.ProviderId == providerResult.Provider.Id);

                // Ignore calculation results with no funding and no existing result
                if (existingResult == null && providerResult != null && providerResult.AllocationLineResults.All(r => !r.Value.HasValue))
                {
                    continue;
                }

                ProviderSummary coreProvider = coreProviderData.FirstOrDefault(p => p.Id == providerResult.Provider.Id);

                if (coreProvider == null)
                {
                    throw new NonRetriableException($"Could not find provider in core data with id '{providerResult.Provider.Id}'");
                }

                ProviderChangeItem changeItem = new ProviderChangeItem
                {
                    UpdatedProvider = coreProvider
                };

                if (existingResult != null)
                {
                    if (existingResult.HasResultBeenVaried)
                    {
                        // Don't replay an already processed variation
                        continue;
                    }

                    List<VariationReason> variationReasons = new List<VariationReason>();

                    if (coreProvider.Status == ProviderStatusClosed)
                    {
                        changeItem.HasProviderClosed = true;
                        changeItem.ProviderReasonCode = coreProvider.ReasonEstablishmentClosed;
                        changeItem.SuccessorProviderId = coreProvider.Successor;
                        changeItem.DoesProviderHaveSuccessor = !string.IsNullOrWhiteSpace(coreProvider.Successor);
                    }

                    if (existingResult.Provider.Authority != coreProvider.Authority)
                    {
                        changeItem.HasProviderDataChanged = true;
                        variationReasons.Add(VariationReason.AuthorityFieldUpdated);
                    }

                    if (existingResult.Provider.EstablishmentNumber != coreProvider.EstablishmentNumber)
                    {
                        changeItem.HasProviderDataChanged = true;
                        variationReasons.Add(VariationReason.EstablishmentNumberFieldUpdated);
                    }

                    if (existingResult.Provider.DfeEstablishmentNumber != coreProvider.DfeEstablishmentNumber)
                    {
                        changeItem.HasProviderDataChanged = true;
                        variationReasons.Add(VariationReason.DfeEstablishmentNumberFieldUpdated);
                    }

                    if (existingResult.Provider.Name != coreProvider.Name)
                    {
                        changeItem.HasProviderDataChanged = true;
                        variationReasons.Add(VariationReason.NameFieldUpdated);
                    }

                    if (existingResult.Provider.LACode != coreProvider.LACode)
                    {
                        changeItem.HasProviderDataChanged = true;
                        variationReasons.Add(VariationReason.LACodeFieldUpdated);
                    }

                    if (existingResult.Provider.LegalName != coreProvider.LegalName)
                    {
                        changeItem.HasProviderDataChanged = true;
                        variationReasons.Add(VariationReason.LegalNameFieldUpdated);
                    }

                    if (coreProvider.Status != ProviderStatusClosed && !string.IsNullOrWhiteSpace(coreProvider.Successor))
                    {
                        throw new NonRetriableException($"Provider has successor in core provider data but is not set to 'Closed' for provider '{providerResult.Provider.Id}'");
                    }

                    if (!string.IsNullOrWhiteSpace(coreProvider.Successor))
                    {
                        ProviderSummary successorCoreProvider = coreProviderData.FirstOrDefault(p => p.Id == coreProvider.Successor);

                        if (successorCoreProvider == null)
                        {
                            throw new NonRetriableException($"Could not find provider successor in core provider data for provider '{providerResult.Provider.Id}' and successor '{coreProvider.Successor}'");
                        }

                        changeItem.SuccessorProvider = successorCoreProvider;
                    }

                    changeItem.VariationReasons = variationReasons;
                    changeItem.PriorProviderState = providerResult.Provider;
                }
                else
                {
                    if (providerResult.AllocationLineResults.Any(r => r.Value.HasValue))
                    {
                        changeItem.HasProviderOpened = true;
                        changeItem.ProviderReasonCode = coreProvider.ReasonEstablishmentOpened;
                    }
                }

                if (changeItem.HasProviderClosed || changeItem.HasProviderDataChanged || changeItem.HasProviderOpened)
                {
                    if (!changeItems.Any(i => i.UpdatedProvider.Id == changeItem.UpdatedProvider.Id))
                    {
                        changeItems.Add(changeItem);
                    }
                }
            }

            return changeItems;
        }
    }
}
