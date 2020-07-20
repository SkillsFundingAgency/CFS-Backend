using CalculateFunding.Models.FDZ;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Services.FDZ.Interfaces
{
    public interface IOrganisationsRetrievalService
    {
        Task<IEnumerable<PaymentOrganisation>> GetAllOrganisations(int providerSnapshotId);
    }
}