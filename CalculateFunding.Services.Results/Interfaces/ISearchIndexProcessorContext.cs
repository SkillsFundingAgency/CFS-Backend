using Microsoft.Azure.ServiceBus;

namespace CalculateFunding.Services.Results.Interfaces
{
    public interface ISearchIndexProcessorContext
    {
        public Message Message { get; }

        public int DegreeOfParallelism { get; }
    }
}
