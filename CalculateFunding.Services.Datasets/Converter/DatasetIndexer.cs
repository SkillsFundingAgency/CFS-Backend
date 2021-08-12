using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Interfaces.Helpers;
using CalculateFunding.Services.Datasets.Interfaces;
using Polly;
using Serilog;

namespace CalculateFunding.Services.Datasets.Converter
{
    public class DatasetIndexer : IDatasetIndexer
    {
        private readonly ISearchRepository<DatasetVersionIndex> _datasetVersionsSearch;
        private readonly ISearchRepository<DatasetIndex> _datasetsSearch;
        private readonly AsyncPolicy _datasetVersionsSearchResilience;
        private readonly AsyncPolicy _datasetSearchResilience;
        private readonly ILogger _logger;

        public DatasetIndexer(ISearchRepository<DatasetVersionIndex> datasetVersionsSearch,
            ISearchRepository<DatasetIndex> datasetsSearch,
            IDatasetsResiliencePolicies resiliencePolicies,
            ILogger logger)
        {
            Guard.ArgumentNotNull(datasetVersionsSearch, nameof(datasetVersionsSearch));
            Guard.ArgumentNotNull(datasetsSearch, nameof(datasetsSearch));
            Guard.ArgumentNotNull(resiliencePolicies?.DatasetSearchService, nameof(_datasetSearchResilience));
            Guard.ArgumentNotNull(resiliencePolicies?.DatasetVersionSearchService, nameof(_datasetVersionsSearchResilience));
            Guard.ArgumentNotNull(logger, nameof(logger));

            _datasetVersionsSearch = datasetVersionsSearch;
            _datasetsSearch = datasetsSearch;
            _logger = logger;
            _datasetSearchResilience = resiliencePolicies.DatasetSearchService;
            _datasetVersionsSearchResilience = resiliencePolicies.DatasetVersionSearchService;
        }

        public async Task IndexDatasetAndVersion(Dataset dataset)
        {
            Task<IEnumerable<IndexError>> datasetIndexing = IndexDataset(dataset);
            Task<IEnumerable<IndexError>> datasetVersionIndexing = IndexDatasetVersion(dataset);

            await TaskHelper.WhenAllAndThrow(datasetIndexing, datasetVersionIndexing);

            EnsureIndexingSucceeded(nameof(datasetIndexing), datasetIndexing.Result?.ToArray());
            EnsureIndexingSucceeded(nameof(datasetVersionIndexing), datasetVersionIndexing.Result?.ToArray());
        }

        private void EnsureIndexingSucceeded(string indexingName,
            IndexError[] errors)
        {
            if (!errors.AnyWithNullCheck()) return;

            string error = $"Could not complete {indexingName}. {errors.Select(_ => $"{_.Key} - {_.ErrorMessage}").Join("; ")}";

            _logger.Error(error);

            throw new InvalidOperationException(error);
        }

        private async Task<IEnumerable<IndexError>> IndexDataset(Dataset dataset)
        {
            DatasetVersion datasetVersion = dataset.Current;

            return await _datasetSearchResilience.ExecuteAsync(() => _datasetsSearch.Index(new[]
            {
                new DatasetIndex
                {
                    Id = dataset.Id,
                    Name = dataset.Name,
                    DefinitionId = dataset.Definition?.Id,
                    DefinitionName = dataset.Definition?.Name,
                    Status = datasetVersion.PublishStatus.ToString(),
                    LastUpdatedDate = datasetVersion.Date,
                    Description = datasetVersion.Description,
                    Version = datasetVersion.Version,
                    ChangeNote = datasetVersion.Comment,
                    LastUpdatedById = datasetVersion.Author?.Id,
                    LastUpdatedByName = datasetVersion.Author?.Name,
                    FundingStreamId = datasetVersion.FundingStream?.Id,
                    FundingStreamName = datasetVersion.FundingStream?.Name,
                    RelationshipId = dataset.RelationshipId
                }
            }));
        }

        private async Task<IEnumerable<IndexError>> IndexDatasetVersion(Dataset dataset)
        {
            DatasetVersion datasetVersion = dataset.Current;

            return await _datasetVersionsSearchResilience.ExecuteAsync(() => _datasetVersionsSearch.Index(new[]
            {
                new DatasetVersionIndex
                {
                    Id = $"{dataset.Id}-{datasetVersion.Version}",
                    DatasetId = dataset.Id,
                    Name = dataset.Name,
                    Version = datasetVersion.Version,
                    BlobName = datasetVersion.BlobName,
                    ChangeNote = datasetVersion.Comment,
                    ChangeType = datasetVersion.ChangeType.ToString(),
                    DefinitionName = dataset.Definition?.Name,
                    Description = datasetVersion.Description,
                    LastUpdatedDate = datasetVersion.Date,
                    LastUpdatedByName = datasetVersion.Author?.Name,
                    FundingStreamId = datasetVersion.FundingStream?.Id,
                    FundingStreamName = datasetVersion.FundingStream?.Name
                }
            }));
        }
    }
}