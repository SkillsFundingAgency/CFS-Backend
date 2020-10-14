using CalculateFunding.Services.Results.Interfaces;
using Microsoft.Azure.ServiceBus;

namespace CalculateFunding.Services.Results.SearchIndex
{
    public class DefaultSearchIndexProcessorContext : ISearchIndexProcessorContext
    {
        public DefaultSearchIndexProcessorContext(Message message)
        {
            Message = message;
            DegreeOfParallelism = 45;
        }

        public Message Message { get; }

        public int DegreeOfParallelism { get; }
    }
}
