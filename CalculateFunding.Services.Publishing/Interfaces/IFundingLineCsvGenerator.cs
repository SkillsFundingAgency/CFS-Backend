using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IFundingLineCsvGenerator
    {
        Task Run(Message message);
    }
}