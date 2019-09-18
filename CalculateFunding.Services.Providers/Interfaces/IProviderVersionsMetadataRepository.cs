using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Models.Providers;

namespace CalculateFunding.Services.Providers.Interfaces
{
    public interface IProviderVersionsMetadataRepository
    {
        Task<HttpStatusCode> UpsertProviderVersionByDate(ProviderVersionByDate providerVersionByDate);
        Task<HttpStatusCode> CreateProviderVersion(ProviderVersionMetadata providerVersion);
        Task<HttpStatusCode> UpsertMaster(MasterProviderVersion providerVersionMetadataViewModel);
        Task<MasterProviderVersion> GetMasterProviderVersion();
        Task<IEnumerable<ProviderVersionMetadata>> GetProviderVersions(string fundingStream);
        Task<ProviderVersionByDate> GetProviderVersionByDate(int year, int month, int day);
        Task<bool> Exists(string name, string providerVersionTypeString, int version, string fundingStream);
    }
}
