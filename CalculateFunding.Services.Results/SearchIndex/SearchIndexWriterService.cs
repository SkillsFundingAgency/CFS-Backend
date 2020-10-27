using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Jobs;
using CalculateFunding.Services.Results.Interfaces;
using Microsoft.Azure.ServiceBus;
using Serilog;
using System;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Results.SearchIndex
{
    public class SearchIndexWriterService : JobProcessingService, ISearchIndexWriterService
    {
        private readonly ILogger _logger;
        private readonly ISearchIndexProcessorFactory _searchIndexProcessorFactory;

        public SearchIndexWriterService(ILogger logger, IJobManagement jobManagement, ISearchIndexProcessorFactory searchIndexProcessorFactory) : base(jobManagement, logger)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(jobManagement, nameof(jobManagement));
            Guard.ArgumentNotNull(searchIndexProcessorFactory, nameof(searchIndexProcessorFactory));
            _logger = logger;
            _searchIndexProcessorFactory = searchIndexProcessorFactory;
        }

        public override async Task Process(Message message)
        {
            Guard.ArgumentNotNull(message, nameof(message));

            try
            {
                string indexWriterType = message.GetUserProperty<string>("index-writer-type");

                if (string.IsNullOrWhiteSpace(indexWriterType))
                {
                    string errorMessage = $"Index-writer-type missing from SearchIndexWriter job. JobId {Job.Id}";
                    _logger.Error(errorMessage);
                    throw new Exception(errorMessage);
                }

                ISearchIndexProcessor processor = _searchIndexProcessorFactory.CreateProcessor(indexWriterType);
                await processor.Process(message);
            }
            catch (Exception exception)
            {
                _logger.Error(exception, $"Failed to run SearchIndexWriter with exception: {exception.Message}, for job id '{Job.Id}'");
                throw;
            }
        }
    }
}
