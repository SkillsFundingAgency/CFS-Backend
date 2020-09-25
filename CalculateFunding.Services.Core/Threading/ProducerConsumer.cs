using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Interfaces.Threading;
using Serilog;

namespace CalculateFunding.Services.Core.Threading
{
    public class ProducerConsumer<TItem> : IProducerConsumer
    {
        private readonly Channel<TItem> _channel;
        private readonly Consumer[] _consumers;
        private readonly Producer _producer;
        private readonly ILogger _logger;

        public ProducerConsumer(Func<CancellationToken, dynamic, Task<(bool Complete, TItem Item)>> producer,
            Func<CancellationToken, dynamic, TItem, Task> consumer,
            int channelBounds,
            int consumerPoolSize, 
            ILogger logger)
        {
            Guard.ArgumentNotNull(producer, nameof(producer));
            Guard.ArgumentNotNull(consumer, nameof(consumer));
            Guard.ArgumentNotNull(logger, nameof(logger));

            if (consumerPoolSize < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(consumerPoolSize), 
                    "Consumer pool size must be at least 1");
            }
            
            if (channelBounds < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(channelBounds), 
                    "Channel bounds must be at least 1");
            }

            CancellationTokenSource = new CancellationTokenSource();
            ConsumerPoolSize = consumerPoolSize;
            ChannelBounds = channelBounds;
            CancellationToken = CancellationTokenSource.Token;
            
            _logger = logger;
            
            _channel = Channel.CreateBounded<TItem>(channelBounds);
            
            _producer = new Producer(producer,
                _channel.Writer,
                CancellationToken, 
                CancellationTokenSource,
                logger);
            
            _consumers = new Consumer[consumerPoolSize];
            
            for (int consumerIndex = 0; consumerIndex < consumerPoolSize; consumerIndex++)
            {
                _consumers[consumerIndex] = new Consumer(consumer,
                    _channel.Reader,
                    CancellationToken,
                    CancellationTokenSource,
                    logger);
            }
        }
        
        public int ConsumerPoolSize { get; }
        
        public int ChannelBounds { get; }
        
        public CancellationToken CancellationToken { get; }
        
        public CancellationTokenSource CancellationTokenSource { get; }

        public async Task Run(dynamic context)
        {
            int consumersLength = _consumers.Length;
            
            Task[] tasks = new Task[consumersLength + 1];
            
            tasks[0] = _producer.ProduceAllItems(context);

            string itemType = typeof(TItem).GetFriendlyName();
            
            _logger.Information($"Started producer task for {itemType}");
            
            for (int consumerIndex = 0; consumerIndex < consumersLength; consumerIndex++)
            {
                tasks[consumerIndex + 1] = _consumers[consumerIndex].Run(context);
            }
            
            _logger.Information($"Started {consumersLength} consumer tasks for {itemType}");

            await TaskHelper.WhenAllAndThrow(tasks);
            
            _logger.Information($"Completed all producer consumer tasks for {itemType}");
            
            await _channel.Reader.Completion;
        }

        private class Producer
        {
            private readonly Func<CancellationToken, dynamic, Task<(bool, TItem)>> _producer;
            private readonly ChannelWriter<TItem> _channelWriter;
            private readonly CancellationToken _cancellationToken;
            private readonly CancellationTokenSource _cancellationTokenSource;
            private readonly ILogger _logger;

            public Producer(Func<CancellationToken, dynamic, Task<(bool, TItem)>> producer,
                ChannelWriter<TItem> channelWriter, 
                CancellationToken cancellationToken, 
                CancellationTokenSource cancellationTokenSource,
                ILogger logger)
            {
                _producer = producer;
                _channelWriter = channelWriter;
                _cancellationToken = cancellationToken;
                _cancellationTokenSource = cancellationTokenSource;
                _logger = logger;
            }

            public async Task ProduceAllItems(object context)
            {
                try
                {
                    while (!_cancellationToken.IsCancellationRequested)
                    {
                        _logger.Information("Running producer delegate");

                        (bool complete, TItem item) = await _producer(_cancellationToken, context);

                        if (complete || !await _channelWriter.WaitToWriteAsync(_cancellationToken))
                        {
                            _logger.Information("Completing producer Channel Writer");

                            return;
                        }

                        _logger.Information("Writing producer results to channel writer");

                        await _channelWriter.WriteAsync(item, _cancellationToken);
                    }
                }
                catch (TaskCanceledException e)
                {
                    _logger.Warning(e, "Unable to complete producer thread, Task cancelled");
                }
                catch (Exception e)
                {
                    _logger.Warning(e, "Unable to complete producer thread. Cancelling producer consumer");
                    
                    _cancellationTokenSource.Cancel();

                    throw;
                }
                finally
                {
                    _channelWriter.TryComplete();    
                }
            }
        }

        private class Consumer
        {
            private readonly Func<CancellationToken, dynamic, TItem, Task> _consumer;
            private readonly ChannelReader<TItem> _reader;
            private readonly CancellationToken _cancellationToken;
            private readonly CancellationTokenSource _cancellationTokenSource;
            private readonly ILogger _logger;

            public Consumer(Func<CancellationToken, dynamic, TItem, Task> consumer, 
                ChannelReader<TItem> reader,
                CancellationToken cancellationToken,
                CancellationTokenSource cancellationTokenSource,
                ILogger logger)
            {
                _reader = reader;
                _cancellationToken = cancellationToken;
                _cancellationTokenSource = cancellationTokenSource;
                _logger = logger;
                _consumer = consumer;
            }

            public async Task Run(object context)
            {
                try
                {
                    while (!_cancellationToken.IsCancellationRequested && await _reader.WaitToReadAsync(_cancellationToken))
                    {
                        if (!_reader.TryRead(out TItem item))
                        {
                            continue;
                        }

                        await _consumer(_cancellationToken, context, item);
                    }
                }
                catch (TaskCanceledException e)
                {
                    _logger.Warning(e, "Unable to complete consumer thread, Task cancelled");
                } 
                catch (Exception e)
                {
                    _logger.Error(e, "Unable to complete consumer thread. Cancelling producer consumer");

                    _cancellationTokenSource.Cancel();
                    
                    throw;
                }
            }
        }
    }
}