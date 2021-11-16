using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using CalculateFunding.Common.ApiClient.DataSets;
using CalculateFunding.Common.ApiClient.DataSets.Models;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Specs;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Interfaces.Threading;
using CalculateFunding.Services.Core.Threading;
using CalculateFunding.Services.Specs.Interfaces;
using Polly;
using Serilog;

namespace CalculateFunding.Services.Specs
{
    public class SpecificationIndexer : ISpecificationIndexer
    {
        private static readonly DatasetSpecificationRelationshipViewModel[] EmptyRelationshipArray = new DatasetSpecificationRelationshipViewModel[0];

        private readonly IDatasetsApiClient _datasets;
        private readonly ISearchRepository<SpecificationIndex> _specificationSearch;
        private readonly AsyncPolicy _datasetsPolicy;
        private readonly AsyncPolicy _specificationsSearchPolicy;
        private readonly IProducerConsumerFactory _producerConsumerFactory;
        private readonly IMapper _mapper;
        private readonly ILogger _logger;

        public SpecificationIndexer(IMapper mapper,
            ISpecificationsResiliencePolicies resiliencePolicies,
            IDatasetsApiClient datasets,
            ISearchRepository<SpecificationIndex> specificationSearch,
            IProducerConsumerFactory producerConsumerFactory,
            ILogger logger)
        {
            Guard.ArgumentNotNull(mapper, nameof(mapper));
            Guard.ArgumentNotNull(resiliencePolicies?.DatasetsApiClient, nameof(resiliencePolicies.DatasetsApiClient));
            Guard.ArgumentNotNull(resiliencePolicies?.SpecificationsSearchRepository, nameof(resiliencePolicies.SpecificationsSearchRepository));
            Guard.ArgumentNotNull(datasets, nameof(datasets));
            Guard.ArgumentNotNull(specificationSearch, nameof(specificationSearch));
            Guard.ArgumentNotNull(producerConsumerFactory, nameof(producerConsumerFactory));
            Guard.ArgumentNotNull(logger, nameof(logger));

            _mapper = mapper;
            _datasetsPolicy = resiliencePolicies.DatasetsApiClient;
            _specificationsSearchPolicy = resiliencePolicies.SpecificationsSearchRepository;
            _datasets = datasets;
            _producerConsumerFactory = producerConsumerFactory;
            _logger = logger;
            _specificationSearch = specificationSearch;
        }

        public async Task Index(Specification specification)
        {
            await IndexSpecifications(new[]
            {
                specification
            });
        }

        public async Task Index(IEnumerable<Specification> specifications)
        {
            await IndexSpecifications(specifications);
        }

        public async Task Index(IEnumerable<SpecificationSearchModel> specifications)
        {
            await IndexSpecifications(specifications);
        }

        public async Task Remove(IEnumerable<SpecificationIndex> specsToRemove)
        {
            await _specificationSearch.Remove(specsToRemove);
        }

        private IEnumerable<SpecificationIndex> GetSearchIndices<TSource>(IEnumerable<TSource> sourceItems)
            => _mapper.Map<IEnumerable<SpecificationIndex>>(sourceItems);

        private async Task IndexSpecifications<TSourceItem>(IEnumerable<TSourceItem> specifications)
        {
            IEnumerable<SpecificationIndex> searchIndices = GetSearchIndices(specifications);

            PagedContext<SpecificationIndex> context = new PagedContext<SpecificationIndex>(searchIndices);

            IProducerConsumer producerConsumer = _producerConsumerFactory.CreateProducerConsumer(ProduceSpecificationIndices,
                IndexSpecifications,
                20,
                1,
                _logger);

            await producerConsumer.Run(context);
        }

        private async Task<(bool isComplete, IEnumerable<SpecificationIndex> items)> ProduceSpecificationIndices(CancellationToken token,
            dynamic context)
        {
            PagedContext<SpecificationIndex> indexingContext = (PagedContext<SpecificationIndex>) context;

            while (indexingContext.HasPages)
            {
                SpecificationIndex[] searchIndices = indexingContext.NextPage();

                foreach (SpecificationIndex specificationIndex in searchIndices)
                {
                    string specificationId = specificationIndex.Id;
                    
                    LogInformation($"Querying dataset relationships for specification {specificationId}");
                    
                    ApiResponse<IEnumerable<DatasetSpecificationRelationshipViewModel>> response =
                        await _datasetsPolicy.ExecuteAsync(() => _datasets.GetRelationshipsBySpecificationId(specificationId));

                    if (response?.StatusCode.IsSuccess() == false || response?.Content == null)
                    {
                        LogError($"Unable to retrieve dataset relationships for specification {specificationId}");
                    }

                    DatasetSpecificationRelationshipViewModel[] mappedDatasets = response?.Content?.Where(_ => _.DatasetId.IsNotNullOrWhitespace()).ToArray() ??
                                                                                 EmptyRelationshipArray;

                    DateTimeOffset? latestMappedUpdatedDate = mappedDatasets.Max(_ => _.LastUpdatedDate);
                    int mappedCount = mappedDatasets.Length;
                    
                    LogInformation(
                        $"Setting total mapped datasets count {mappedCount} and map dataset last updated {latestMappedUpdatedDate} on search index for specification id {specificationId}");

                    specificationIndex.TotalMappedDataSets = mappedCount;
                    specificationIndex.MapDatasetLastUpdated = latestMappedUpdatedDate;
                }

                return (false, searchIndices);
            }

            return (true, ArraySegment<SpecificationIndex>.Empty);
        }

        private async Task IndexSpecifications(CancellationToken cancellationToken,
            dynamic context,
            IEnumerable<SpecificationIndex> specifications)
        {
            LogInformation($"Indexing next {specifications.Count()} specifications");
            
            IEnumerable<IndexError> indexingErrors = await _specificationsSearchPolicy.ExecuteAsync(() => _specificationSearch.Index(specifications));
            
            if (!indexingErrors.IsNullOrEmpty())
            {
                string indexingErrorMessages = indexingErrors.Select(_ => _.ErrorMessage).Join(". ");
                string specificationIds =   specifications.Select(_ => _.Id).Join(", ");

                LogError($"Could not index specifications {specificationIds} because: {indexingErrorMessages}");
                
                throw new FailedToIndexSearchException(indexingErrors);
            }
        }

        private void LogError(string message) => _logger.Error(FormatLogMessage(message));
        
        private void LogInformation(string message) => _logger.Information(FormatLogMessage(message));

        private static string FormatLogMessage(string message) => $"SpecificationIndexer: {message}";

        public async Task<ServiceHealth> IsHealthOk()
        {
            (bool Ok, string Message) searchRepoHealth = await _specificationSearch.IsHealthOk();

            ServiceHealth serviceHealth = new ServiceHealth
            {
                Name = nameof(SpecificationIndexer)
            };
            
            serviceHealth.Dependencies.Add( new DependencyHealth
            {
                HealthOk = searchRepoHealth.Ok, 
                DependencyName = _specificationSearch.GetType().GetFriendlyName(),
                Message = searchRepoHealth.Message
            });
            
            return serviceHealth;
        }
    }
}