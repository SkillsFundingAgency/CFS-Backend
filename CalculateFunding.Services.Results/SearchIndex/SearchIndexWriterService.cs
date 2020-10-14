using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Results.Interfaces;
using Microsoft.Azure.ServiceBus;
using Serilog;
using System;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Results.SearchIndex
{
    public class SearchIndexWriterService : ISearchIndexWriterService
    {
        private readonly ILogger _logger;
        private readonly IJobManagement _jobManagement;
        private readonly ISearchIndexProcessorFactory _searchIndexProcessorFactory;

        public SearchIndexWriterService(ILogger logger, IJobManagement jobManagement, ISearchIndexProcessorFactory searchIndexProcessorFactory)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(jobManagement, nameof(jobManagement));
            Guard.ArgumentNotNull(searchIndexProcessorFactory, nameof(searchIndexProcessorFactory));
            _logger = logger;
            _jobManagement = jobManagement;
            _searchIndexProcessorFactory = searchIndexProcessorFactory;
        }

        public async Task CreateSearchIndex(Message message)
        {
            Guard.ArgumentNotNull(message, nameof(message));

            string jobId = message.GetUserProperty<string>("jobId");

            JobViewModel jobResponse = await _jobManagement.GetJobById(jobId);

            if (jobResponse == null)
            {
                string errorMessage = $"Error occurred while retireving the job. JobId {jobId}";
                _logger.Error(errorMessage);
                throw new Exception(errorMessage);
            }

            string indexWriterType = message.GetUserProperty<string>("index-writer-type");

            if (string.IsNullOrWhiteSpace(indexWriterType))
            {
                string errorMessage = $"Index-writer-type missing from SearchIndexWriter job. JobId {jobId}";
                _logger.Error(errorMessage);
                throw new Exception(errorMessage);
            }

            try
            {
                ISearchIndexProcessor processor = _searchIndexProcessorFactory.CreateProcessor(indexWriterType);
                await processor.Process(message);

                await _jobManagement.UpdateJobStatus(jobId, 100, true, $"SearchIndexWriter completed.");
            }
            catch (NonRetriableException ex)
            {
                _logger.Error(ex, $"Failed to run SearchIndexWriter with exception: {ex.Message}, for job id '{jobId}'");
                await _jobManagement.UpdateJobStatus(jobId, 100, false, $"Failed to run SearchIndexWriter - {ex.Message}");
            }
            catch (Exception exception)
            {
                _logger.Error(exception, $"Failed to run SearchIndexWriter with exception: {exception.Message}, for job id '{jobId}'");
                throw;
            }
        }
    }
}
