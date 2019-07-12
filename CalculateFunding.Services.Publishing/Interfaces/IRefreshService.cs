using Microsoft.Azure.ServiceBus;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IRefreshService
    {
        Task RefreshResults(Message message);
    }
}
