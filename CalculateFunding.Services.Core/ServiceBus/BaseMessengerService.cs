using CalculateFunding.Services.Core.Interfaces.ServiceBus;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Core.ServiceBus
{
    public abstract class BaseMessengerService
    {
        public async Task<IEnumerable<T>> ReceiveMessages<T>(string entityPath, TimeSpan timeout) where T : class
        {
            List<T> messages = new List<T>();
            await ReceiveMessages<T>(entityPath, _ =>
            {
                messages.Add(_);
                return false;
            },
            timeout);
            return messages;
        }

        public async Task<T> ReceiveMessage<T>(string entityPath, Predicate<T> predicate, TimeSpan timeout) where T : class
        {
            T message = null;
            await ReceiveMessages<T>(entityPath, _ =>
            {
                if (predicate(_))
                {
                    message = _;
                    return true;
                }
                return false;
            },
            timeout);
            return message;
        }

        protected abstract Task ReceiveMessages<T>(string entityPath, Predicate<T> predicate, TimeSpan timeout) where T : class;
    }
}
