using CalculateFunding.Tests.Common.Helpers;
using Microsoft.Azure.ServiceBus;

namespace CalculateFunding.Services.Publishing.Services.UnitTests
{
    public class MessageBuilder : TestEntityBuilder
    {
        private readonly Message _message = new Message();

        public MessageBuilder WithUserProperty(string key, string value)
        {
            _message.UserProperties.Add(key, value);

            return this;
        }

        public MessageBuilder WithoutUserProperty(string key)
        {
            _message.UserProperties.Remove(key);

            return this;
        }
        
        public Message Build()
        {
            return _message;
        }
    }
}