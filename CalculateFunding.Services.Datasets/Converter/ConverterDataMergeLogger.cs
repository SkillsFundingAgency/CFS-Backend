using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Datasets.Converter;
using CalculateFunding.Services.Datasets.Interfaces;
using Polly;

namespace CalculateFunding.Services.Datasets.Converter
{
    public class ConverterDataMergeLogger : IConverterDataMergeLogger
    {
        private readonly IDatasetRepository _datasets;
        private AsyncPolicy _datasetsResilience;

        public ConverterDataMergeLogger(IDatasetRepository datasets,
            IDatasetsResiliencePolicies resiliencePolicies)
        {
            Guard.ArgumentNotNull(datasets, nameof(datasets));
            Guard.ArgumentNotNull(resiliencePolicies?.DatasetRepository, nameof(_datasetsResilience));
            
            _datasets = datasets;
            _datasetsResilience = resiliencePolicies.DatasetRepository;
        }

        public async Task SaveLogs(IEnumerable<RowCopyResult> results,
            ConverterMergeRequest request,
            string jobId,
            int datasetVersionCreated)
        {
            ConverterDataMergeLog log = new ConverterDataMergeLog
            {
                Request = request,
                Results = results,
                JobId = jobId,
                DatasetVersionCreated = datasetVersionCreated
            };
            
            await _datasetsResilience.ExecuteAsync(() => _datasets.SaveConverterDataMergeLog(log));
        }
    }
}
