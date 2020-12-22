using System.Threading.Tasks;

namespace CalculateFunding.Services.Providers.Interfaces
{
    public interface IProviderVersionUpdateCheckService
    {
        Task CheckProviderVersionUpdate();
    }
}
