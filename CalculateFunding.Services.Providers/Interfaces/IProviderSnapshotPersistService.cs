using CalculateFunding.Common.ApiClient.FundingDataZone.Models;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Providers.Interfaces
{
    public interface IProviderSnapshotPersistService
    {
        Task<bool> PersistSnapshot(ProviderSnapshot providerSnapshot);
    }
}
