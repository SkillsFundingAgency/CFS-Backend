using CalculateFunding.Common.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Publishing.Interfaces;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing
{
    public class RefreshStateService : IRefreshStateService
    {
        private readonly ILogger _logger;
        private readonly IPublishedProviderStatusUpdateService _publishedProviderStatusUpdateService;
        private readonly IPublishedProviderIndexerService _publishedProviderIndexerService;
        private readonly IPublishedFundingDataService _publishedFundingDataService;

        private enum ActionType
        {
            Add,
            Update,
            Delete
        }

        private IDictionary<ActionType, IDictionary<string, PublishedProvider>> _providers = new Dictionary<ActionType, IDictionary<string, PublishedProvider>>();

        public RefreshStateService(ILogger logger,
            IPublishedProviderStatusUpdateService publishedProviderStatusUpdateService,
            IPublishedProviderIndexerService publishedProviderIndexerService,
            IPublishedFundingDataService publishedFundingDataService)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(publishedProviderStatusUpdateService, nameof(publishedProviderStatusUpdateService));
            Guard.ArgumentNotNull(publishedProviderIndexerService, nameof(publishedProviderIndexerService));
            Guard.ArgumentNotNull(publishedFundingDataService, nameof(publishedFundingDataService));

            _logger = logger;
            _publishedProviderStatusUpdateService = publishedProviderStatusUpdateService;
            _publishedProviderIndexerService = publishedProviderIndexerService;
            _publishedFundingDataService = publishedFundingDataService;
        }

        public void AddRange(IDictionary<string, PublishedProvider> publishedProviders)
        {
            AddRange(ActionType.Add, publishedProviders);
        }

        public IDictionary<string, PublishedProvider> NewProviders => _providers.ContainsKey(ActionType.Add) ? _providers[ActionType.Add] : null;

        public IEnumerable<PublishedProvider> UpdatedProviders => _providers.ContainsKey(ActionType.Update) ? _providers[ActionType.Update].Values : ArraySegment<PublishedProvider>.Empty;

        public bool IsNewProvider(PublishedProvider publishedProvider) => _providers.ContainsKey(ActionType.Add) ? _providers[ActionType.Add].ContainsKey(publishedProvider.Current.Id) : false;

        public int Count => _providers.Values.SelectMany(_ => _.Values).Count();

        public void Add(PublishedProvider publishedProvider)
        {
            Add(ActionType.Add, publishedProvider);
        }

        public void Update(PublishedProvider publishedProvider)
        {
            Add(ActionType.Update, publishedProvider);
        }

        public void Delete(PublishedProvider publishedProvider)
        {
            Add(ActionType.Delete, publishedProvider);
        }

        public bool Exclude(PublishedProvider publishedProvider)
        {
            bool exclude = false;

            if (NewProviders != null && NewProviders.ContainsKey(publishedProvider.Current.Id))
            {
                NewProviders.Remove(publishedProvider.Current.Id);
                exclude = true;
            }

            return exclude;
        }

        public async Task Persist(string jobId, Reference author, string correlationId)
        {
            if (_providers.Count > 0)
            {
                foreach(ActionType actionType in _providers.Keys)
                {
                    IDictionary<string, PublishedProvider> providers = _providers[actionType];

                    if (_providers[actionType].Count > 0)
                    {
                        switch (actionType)
                        {
                            case ActionType.Update:
                                {
                                    _logger.Information($"Saving updates to existing published providers. Total={providers.Count}");

                                    IEnumerable<PublishedProvider> draftPublishedProviders = providers.Values.Where(_ => _.Current.Status == PublishedProviderStatus.Draft);
                                    IEnumerable<PublishedProvider> nonDraftPublishedProviders = providers.Values.Except(draftPublishedProviders);

                                    await _publishedProviderStatusUpdateService.UpdatePublishedProviderStatus(nonDraftPublishedProviders, author, PublishedProviderStatus.Updated, jobId, correlationId);
                                    await _publishedProviderStatusUpdateService.UpdatePublishedProviderStatus(draftPublishedProviders, author, PublishedProviderStatus.Draft, jobId, correlationId);

                                    _logger.Information("Indexing existing PublishedProviders");
                                    await _publishedProviderIndexerService.IndexPublishedProviders(providers.Values.Select(_ => _.Current));

                                    break;
                                }
                            case ActionType.Add:
                                {
                                    _logger.Information($"Saving new published providers. Total={providers.Count}");
                                    await _publishedProviderStatusUpdateService.UpdatePublishedProviderStatus(providers.Values, author, PublishedProviderStatus.Draft, jobId, correlationId);

                                    _logger.Information("Indexing newly added PublishedProviders");
                                    await _publishedProviderIndexerService.IndexPublishedProviders(providers.Values.Select(_ => _.Current));

                                    break;
                                }
                            case ActionType.Delete:
                                {
                                    _logger.Information($"Deleting existing published providers. Total={providers.Count}");
                                    await _publishedFundingDataService.DeletePublishedProviders(providers.Values);

                                    _logger.Information("Removing index of existing PublishedProviders");
                                    await _publishedProviderIndexerService.Remove(providers.Values.Select(_ => _.Current));

                                    break;
                                }
                        }
                    }
                }
            }
        }

        private void AddRange(ActionType actionType, IDictionary<string, PublishedProvider> publishedProviders)
        {
            Get(actionType).AddRange(publishedProviders);
        }
        
        private void Add(ActionType actionType, PublishedProvider publishedProvider)
        {
            if (actionType == ActionType.Update && NewProviders != null && NewProviders.ContainsKey(publishedProvider.Current.ProviderId))
            {
                return;
            }

            Get(actionType)[publishedProvider.Current.ProviderId] = publishedProvider;
        }

        private IDictionary<string, PublishedProvider> Get(ActionType actionType)
        {
            IDictionary<string, PublishedProvider> providers;
            if (!_providers.TryGetValue(actionType, out providers))
            {
                providers = new Dictionary<string, PublishedProvider>();
                _providers.Add(actionType, providers);
            }

            return providers;
        }
    }
}
