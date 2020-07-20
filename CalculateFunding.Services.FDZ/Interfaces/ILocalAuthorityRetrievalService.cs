using CalculateFunding.Models.FDZ;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Services.FDZ.Interfaces
{
    public interface ILocalAuthorityRetrievalService
    {
        Task<IEnumerable<PaymentOrganisation>> GetLocalAuthorities(int providerSnapshotId);
    }
}