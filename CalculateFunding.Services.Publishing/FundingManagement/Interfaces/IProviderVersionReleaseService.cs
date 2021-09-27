using CalculateFunding.Common.Sql.Interfaces;
using CalculateFunding.Models.Publishing;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.FundingManagement.Interfaces
{
    public interface IProviderVersionReleaseService
    {
        Task ReleaseProviderVersions(IEnumerable<PublishedProviderVersion> providerVersions, string specificationId);
    }
}
