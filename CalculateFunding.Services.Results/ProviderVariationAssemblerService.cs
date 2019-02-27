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

        private readonly IPublishedProviderResultsRepository _publishedProviderResultsRepository;
        private readonly IProviderService _providerService;

        public ProviderVariationAssemblerService(IPublishedProviderResultsRepository publishedProviderResultsRepository, IProviderService providerService)
        {
            Guard.ArgumentNotNull(publishedProviderResultsRepository, nameof(publishedProviderResultsRepository));
            Guard.ArgumentNotNull(providerService, nameof(providerService));

            _publishedProviderResultsRepository = publishedProviderResultsRepository;
            _providerService = providerService;
        }

        public async Task<IEnumerable<ProviderChangeItem>> AssembleProviderVariationItems(IEnumerable<ProviderResult> providerResults, string specificationId)
        {
            List<ProviderChangeItem> changeItems = new List<ProviderChangeItem>();

            Task<IEnumerable<PublishedProviderResultExisting>> existingResultsTask = _publishedProviderResultsRepository.GetExistingPublishedProviderResultsForSpecificationId(specificationId);
            Task<IEnumerable<ProviderSummary>> coreProviderTask = _providerService.FetchCoreProviderData();

            await Task.WhenAll(existingResultsTask, coreProviderTask);

            IEnumerable<PublishedProviderResultExisting> existingPublishedProviderResults = existingResultsTask.Result;
            IEnumerable<ProviderSummary> coreProviderData = coreProviderTask.Result;

            if (coreProviderData.IsNullOrEmpty())
            {
                throw new NonRetriableException("Failed to retrieve core provider data");
            }

            foreach (ProviderResult providerResult in providerResults)
            {
                PublishedProviderResultExisting existingResult = existingPublishedProviderResults.SingleOrDefault(r => r.ProviderId == providerResult.Provider.Id);

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
                    List<VariationReason> variationReasons = new List<VariationReason>();

                    if (coreProvider.Status == ProviderStatusClosed)
                    {
                        changeItem.HasProviderClosed = true;
                        changeItem.ProviderReasonCode = coreProvider.ReasonEstablishmentClosed;
                        changeItem.SuccessorProviderId = coreProvider.Successor;
                        changeItem.DoesProviderHaveSuccessor = !string.IsNullOrWhiteSpace(coreProvider.Successor);
                    }

                    if (providerResult.Provider.Authority != coreProvider.Authority)
                    {
                        changeItem.HasProviderDataChanged = true;
                        variationReasons.Add(VariationReason.AuthorityFieldUpdated);
                    }

                    if (providerResult.Provider.EstablishmentNumber != coreProvider.EstablishmentNumber)
                    {
                        changeItem.HasProviderDataChanged = true;
                        variationReasons.Add(VariationReason.EstablishmentNumberFieldUpdated);
                    }

                    if (providerResult.Provider.DfeEstablishmentNumber != coreProvider.DfeEstablishmentNumber)
                    {
                        changeItem.HasProviderDataChanged = true;
                        variationReasons.Add(VariationReason.DfeEstablishmentNumberFieldUpdated);
                    }

                    if (providerResult.Provider.Name != coreProvider.Name)
                    {
                        changeItem.HasProviderDataChanged = true;
                        variationReasons.Add(VariationReason.NameFieldUpdated);
                    }

                    if (providerResult.Provider.LACode != coreProvider.LACode)
                    {
                        changeItem.HasProviderDataChanged = true;
                        variationReasons.Add(VariationReason.LACodeFieldUpdated);
                    }

                    if (providerResult.Provider.LegalName != coreProvider.LegalName)
                    {
                        changeItem.HasProviderDataChanged = true;
                        variationReasons.Add(VariationReason.LegalNameFieldUpdated);
                    }

                    if (coreProvider.Status != ProviderStatusClosed && !string.IsNullOrWhiteSpace(coreProvider.Successor))
                    {
                        throw new NonRetriableException($"Provider has successor in core provider data but is not set to 'Closed' for provider '{providerResult.Id}'");
                    }

                    if (!string.IsNullOrWhiteSpace(coreProvider.Successor))
                    {
                        ProviderSummary successorCoreProvider = coreProviderData.FirstOrDefault(p => p.Id == coreProvider.Successor);

                        if (successorCoreProvider == null)
                        {
                            throw new NonRetriableException($"Could not find provider successor in core provider data for provider '{providerResult.Id}' and successor '{coreProvider.Successor}'");
                        }

                        changeItem.SuccessorProvider = successorCoreProvider;
                    }

                    changeItem.VariationReasons = variationReasons;
                    changeItem.PriorProviderState = providerResult.Provider;
                }
                else
                {
                    changeItem.HasProviderOpened = true;
                    changeItem.ProviderReasonCode = coreProvider.ReasonEstablishmentOpened;
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
