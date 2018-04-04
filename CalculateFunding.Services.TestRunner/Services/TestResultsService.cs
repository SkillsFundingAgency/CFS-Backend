using AutoMapper;
using CalculateFunding.Models.Results;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.TestRunner.Interfaces;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.TestRunner.Services
{
    public class TestResultsService : ITestResultsService
    {
        private readonly ITestResultsRepository _testResultsRepository;
        private readonly ISearchRepository<TestScenarioResultIndex> _searchRepository;
        private readonly IMapper _mapper;
        private readonly ILogger _logger;

        public TestResultsService(ITestResultsRepository testResultsRepository,
            ISearchRepository<TestScenarioResultIndex> searchRepository, 
            IMapper mapper,
            ILogger logger)
        {
            Guard.ArgumentNotNull(searchRepository, nameof(searchRepository));
            Guard.ArgumentNotNull(testResultsRepository, nameof(testResultsRepository));
            Guard.ArgumentNotNull(mapper, nameof(mapper));
            Guard.ArgumentNotNull(logger, nameof(logger));

            _testResultsRepository = testResultsRepository;
            _searchRepository = searchRepository;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<HttpStatusCode> SaveTestProviderResults(IEnumerable<TestScenarioResult> testResults)
        {
            Guard.ArgumentNotNull(testResults, nameof(testResults));

            if (!testResults.Any())
            {
                return HttpStatusCode.NotModified;
            }

            Task<HttpStatusCode> repoUpdateTask = _testResultsRepository.SaveTestProviderResults(testResults);

            IEnumerable<TestScenarioResultIndex> searchIndexItems = _mapper.Map<IEnumerable<TestScenarioResultIndex>>(testResults);
            Task<IEnumerable<IndexError>> searchUpdateTask = _searchRepository.Index(searchIndexItems);

            await TaskHelper.WhenAllAndThrow(searchUpdateTask, repoUpdateTask);

            IEnumerable<IndexError> indexErrors = searchUpdateTask.Result;
            HttpStatusCode repositoryUpdateStatusCode = repoUpdateTask.Result;

            if(!indexErrors.Any() && (repoUpdateTask.Result == HttpStatusCode.Created || repoUpdateTask.Result  == HttpStatusCode.NotModified))
            {
                return HttpStatusCode.Created;
            }

            foreach(IndexError indexError in indexErrors)
            {
                _logger.Error($"SaveTestProviderResults index error {{key}}: {indexError.ErrorMessage}", indexError.Key);
            }

            if(repositoryUpdateStatusCode == default(HttpStatusCode))
            {
                _logger.Error("SaveTestProviderResults repository failed with no response code");
            }
            else
            {
                _logger.Error("SaveTestProviderResults repository failed with response code: {repositoryUpdateStatusCode}", repositoryUpdateStatusCode);

            }

            return HttpStatusCode.InternalServerError;
        }
    }
}
