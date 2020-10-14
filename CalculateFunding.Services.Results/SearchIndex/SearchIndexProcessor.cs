using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Results.Interfaces;
using Microsoft.Azure.ServiceBus;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Results.SearchIndex
{
    public abstract class SearchIndexProcessor<TKey, TInput, TOutput> : ISearchIndexProcessor
        where TInput : class where TOutput : class
    {
        private readonly ILogger _logger;
        private readonly ISearchIndexDataReader<TKey, TInput> _reader;
        private readonly ISearchIndexTrasformer<TInput, TOutput> _transformer;
        private readonly ISearchRepository<TOutput> _searchRepository;

        protected SearchIndexProcessor(
            ILogger logger,
            ISearchIndexDataReader<TKey, TInput> reader,
            ISearchIndexTrasformer<TInput, TOutput> transformer,
            ISearchRepository<TOutput> searchRepository)
        {
            _logger = logger;
            _reader = reader;
            _transformer = transformer;
            _searchRepository = searchRepository;
        }

        public abstract string IndexWriterType { get; }
        protected abstract string IndexName { get; }

        public async Task Process(Message message)
        {
            List<string> exceptionMessages = new List<string>();
            ISearchIndexProcessorContext context = CreateContext(message);

            SemaphoreSlim throttler = new SemaphoreSlim(context.DegreeOfParallelism, context.DegreeOfParallelism);
            List<Task> indexTasks = new List<Task>();

            foreach (TKey key in IndexDataItemKeys(context))
            {
                await throttler.WaitAsync();

                indexTasks.Add
                (
                    Task.Run(async () =>
                        {
                            try
                            {
                                TInput indexData = await _reader.GetData(key);

                                TOutput indexDocument = await _transformer.Transform(indexData, context);

                                IEnumerable<IndexError> results = await _searchRepository.Index(new[] { indexDocument });

                                if (!results.IsNullOrEmpty())
                                {
                                    IndexError indexError = results.First(); // Only indexing one document
                                    exceptionMessages.Add($"{indexError.Key}:{indexError.ErrorMessage}");
                                }
                            }
                            catch (Exception ex)
                            {
                                if (ex is AggregateException)
                                {
                                    ex = ((AggregateException)ex).Flatten().InnerExceptions.FirstOrDefault() ?? ex;
                                }

                                _logger.Error(ex, $"Error occurred while processing the {IndexName} - {ex.Message}");
                                exceptionMessages.Add(ex.Message);
                            }
                            finally
                            {
                                throttler.Release();
                            }
                        })
                );
            }

            await TaskHelper.WhenAllAndThrow(indexTasks.ToArray());

            if (exceptionMessages.Any())
            {
                throw new Exception($"Error occurred while processing the {IndexName}. {string.Join(";", exceptionMessages)}");
            }
        }

        protected abstract IEnumerable<TKey> IndexDataItemKeys(ISearchIndexProcessorContext context);

        protected virtual ISearchIndexProcessorContext CreateContext(Message message)
        {
            return new DefaultSearchIndexProcessorContext(message);
        }
    }
}
