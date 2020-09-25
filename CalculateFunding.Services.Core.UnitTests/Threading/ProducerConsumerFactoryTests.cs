using System.Threading;
using CalculateFunding.Services.Core.Interfaces.Threading;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Serilog.Core;

namespace CalculateFunding.Services.Core.Threading
{
    [TestClass]
    public class ProducerConsumerFactoryTests : ProducerConsumerTestBase
    {
        private ProducerConsumerFactory _factory;

        [TestInitialize]
        public void SetUp()
        {
            _factory = new ProducerConsumerFactory();
        }
        
        [TestMethod]
        public void CreatesConcreteProducerConsumer()
        {
            int expectedChannelBounds = NewRandomNumber();
            int expectedConsumerPoolSize = NewRandomNumber();

            IProducerConsumer producerConsumer = _factory.CreateProducerConsumer(Producer,
                Consumer,
                expectedChannelBounds,
                expectedConsumerPoolSize,
                Logger.None);

            producerConsumer
                .Should()
                .BeOfType<ProducerConsumer<string>>();

            producerConsumer
                .ChannelBounds
                .Should()
                .Be(expectedChannelBounds);

            producerConsumer
                .ConsumerPoolSize
                .Should()
                .Be(expectedConsumerPoolSize);
        }
    }
}