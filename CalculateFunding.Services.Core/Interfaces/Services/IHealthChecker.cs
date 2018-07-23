using CalculateFunding.Models.Health;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Core.Interfaces.Services
{
    public interface IHealthChecker
    {
        Task<ServiceHealth> IsHealthOk();
    }
}
