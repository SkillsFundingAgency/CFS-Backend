using System;
using System.Threading;
using System.Threading.Tasks;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Serilog.Core;

namespace CalculateFunding.Services.Core.Threading
{
    [TestClass]
    public class ProducerConsumerTest : ProducerConsumerTestBase
    {
        private ProducerConsumer<string> _producerConsumer;
        
        [TestMethod]
        public async Task ProcessesAllProducedItems()
        {
            string[] expectedProcessedItems = new[]
            {
                NewRandomString(),
                NewRandomString(),
                NewRandomString(),
                NewRandomString(),
                NewRandomString()
            };
            
            GivenTheProducerConsumer(Producer,
                Consumer,
                2,
                2);
            AndTheItemsToProduce(expectedProcessedItems);

            await WhenTheProducerConsumerIsRun();

            ProcessedItems
                .Should()
                .BeEquivalentTo(expectedProcessedItems,
                    cfg => cfg.WithoutStrictOrdering());
        }

        [TestMethod]
        public void GuardsAgainstMissingProducerFunc()
        {
            Action invocation = () => GivenTheProducerConsumer(null, 
                Consumer, 
                1, 
                1);

            invocation
                .Should()
                .Throw<ArgumentNullException>()
                .Which
                .ParamName
                .Should()
                .Be("producer");
        }

        [TestMethod]
        public void GuardsAgainstMissingConsumerFunc()
        {
            Action invocation = () => GivenTheProducerConsumer(Producer, 
                null, 
                1, 
                1);

            invocation
                .Should()
                .Throw<ArgumentNullException>()
                .Which
                .ParamName
                .Should()
                .Be("consumer");   
        }
        
        [TestMethod]
        public void GuardsAgainstChannelBoundsLessThan1()
        {
            Action invocation = () => GivenTheProducerConsumer(Producer, 
                Consumer, 
                0, 
                1);

            invocation
                .Should()
                .Throw<ArgumentOutOfRangeException>()
                .Which
                .ParamName
                .Should()
                .Be("channelBounds");      
        }
        
        [TestMethod]
        public void GuardsAgainstConsumerPoolSizeLessThan1()
        {
            Action invocation = () => GivenTheProducerConsumer(Producer, 
                Consumer, 
                1, 
                0);

            invocation
                .Should()
                .Throw<ArgumentOutOfRangeException>()
                .Which
                .ParamName
                .Should()
                .Be("consumerPoolSize");          
        }

        private async Task WhenTheProducerConsumerIsRun()
        {
            await _producerConsumer.Run(null);
        }
        
        private void GivenTheProducerConsumer(Func<CancellationToken, dynamic, Task<(bool Complete, string Item)>> producer,
            Func<CancellationToken, dynamic, string, Task> consumer,
            int channelBounds,
            int consumerPoolSize)
        {
            _producerConsumer = new ProducerConsumer<string>(producer,
                consumer,
                channelBounds,
                consumerPoolSize,
                Logger.None);
        }

        private void AndTheItemsToProduce(params string[] items)
        {
            foreach (string item in items)
            {
                ItemsToProduce.Enqueue(item);
            }
        }
        
        private string NewRandomString() => new RandomString();
    }
}