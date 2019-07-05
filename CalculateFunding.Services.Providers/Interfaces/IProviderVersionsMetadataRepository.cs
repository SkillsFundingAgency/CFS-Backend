using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Models.Providers;

namespace CalculateFunding.Services.Providers.Interfaces
{
    public interface IProviderVersionsMetadataRepository
    {
        Task<HttpStatusCode> UpsertProviderVersionByDate(ProviderVersionByDate providerVersionByDate);
        Task<HttpStatusCode> CreateProviderVersion(ProviderVersion providerVersion);
        Task<HttpStatusCode> UpsertMaster(MasterProviderVersion providerVersionMetadataViewModel);
        Task<MasterProviderVersion> GetMasterProviderVersion();
        Task<ProviderVersionByDate> GetProviderVersionByDate(int year, int month, int day);
        Task<bool> Exists(string name, string providerVersionTypeString, int version, string fundingStream);
    }
}
