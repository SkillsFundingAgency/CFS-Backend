using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Tests.Common.Helpers;
using Microsoft.Azure.ServiceBus;

namespace CalculateFunding.Services.Results.UnitTests
{
    public class MessageBuilder : TestEntityBuilder
    {
        private ICollection<(string key, string value)> _properties = new List<(string key, string value)>();
        private byte[] _messageBody;

        public MessageBuilder WithMessageBody(byte[] messageBody)
        {
            _messageBody = messageBody;

            return this;
        }

        public MessageBuilder WithUserProperty(string key, string value)
        {
            _properties.Add((key, value));

            return this;
        }

        public Message Build()
        {
            Message message = _messageBody.IsNullOrEmpty() ? new Message() : new Message(_messageBody);

            foreach ((string key, string value) property in _properties)
            {
                message.UserProperties.Add(property.key, property.value);
            }

            return message;
        }
    }
}