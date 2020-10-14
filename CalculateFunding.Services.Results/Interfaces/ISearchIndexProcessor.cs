using Microsoft.Azure.ServiceBus;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Results.Interfaces
{
    public interface ISearchIndexProcessor
    {
        Task Process(Message message);
        string IndexWriterType { get; }
    }
}
