using CalculateFunding.Common.Models;
using CalculateFunding.Models.Providers;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Providers.Interfaces
{
    public interface IProviderVersionsMetadataRepository
    {
        Task<HttpStatusCode> UpsertProviderVersionByDate(ProviderVersionByDate providerVersionByDate);
        Task<HttpStatusCode> UpsertMaster(MasterProviderVersion providerVersionMetadataViewModel);
        Task<MasterProviderVersion> GetMasterProviderVersion();
        Task<ProviderVersionByDate> GetProviderVersionByDate(int year, int month, int day);
    }
}
