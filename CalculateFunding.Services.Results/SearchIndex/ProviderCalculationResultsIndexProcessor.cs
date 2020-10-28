using CalculateFunding.Repositories.Common.Search;
using Microsoft.Azure.ServiceBus;
using Serilog;
using System.Collections.Generic;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Services.Results.Interfaces;
using CalculateFunding.Common.Utility;
using System.Text;
using System;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Results.Models;

namespace CalculateFunding.Services.Results.SearchIndex
{
    public class ProviderCalculationResultsIndexProcessor : SearchIndexProcessor<ProviderResultDataKey, ProviderResult, ProviderCalculationResultsIndex>
    {
        private readonly ISearchIndexWriterSettings _settings;

        public ProviderCalculationResultsIndexProcessor(
            ILogger logger,
            ISearchIndexDataReader<ProviderResultDataKey, ProviderResult> reader,
            ISearchIndexTrasformer<ProviderResult, ProviderCalculationResultsIndex> transformer,
            ISearchRepository<ProviderCalculationResultsIndex> searchRepository,
            ISearchIndexWriterSettings settings) : base(logger, reader, transformer, searchRepository)
        {
            _settings = settings;
        }

        public override string IndexWriterType => SearchIndexWriterTypes.ProviderCalculationResultsIndexWriter;
        protected override string IndexName => nameof(ProviderCalculationResultsIndex);

        protected override ISearchIndexProcessorContext CreateContext(Message message)
        {
            return new ProviderCalculationResultsIndexProcessorContext(message) { DegreeOfParallelism = _settings.ProviderCalculationResultsIndexWriterDegreeOfParallelism };
        }

        protected override IEnumerable<ProviderResultDataKey> IndexDataItemKeys(ISearchIndexProcessorContext context)
        {
            var processorContext = context as ProviderCalculationResultsIndexProcessorContext;
            Guard.ArgumentNotNull(processorContext, nameof(ProviderCalculationResultsIndexProcessorContext));

            foreach (string providerId in processorContext.ProviderIds)
            {
                byte[] providerResultIdBytes = Encoding.UTF8.GetBytes($"{providerId}-{processorContext.SpecificationId}");
                yield return new ProviderResultDataKey(Convert.ToBase64String(providerResultIdBytes), providerId);
            }
        }
    }
}
