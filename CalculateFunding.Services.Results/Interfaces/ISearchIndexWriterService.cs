using Microsoft.Azure.ServiceBus;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Results.Interfaces
{
    public interface ISearchIndexWriterService
    {
        Task CreateSearchIndex(Message message);
    }
}
