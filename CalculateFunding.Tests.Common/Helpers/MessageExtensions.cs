using Microsoft.Azure.ServiceBus;

namespace CalculateFunding.Tests.Common.Helpers
{
    public static class MessageExtensions
    {
        public static void AddUserProperties(this Message message, params (string, string)[] properties)
        {
            foreach ((string, string) property in properties)
            {
                message.UserProperties.Add(property.Item1, property.Item2);
            }
        }
    }
}