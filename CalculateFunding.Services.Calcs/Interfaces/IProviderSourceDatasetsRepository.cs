using CalculateFunding.Models.Results;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Calcs.Interfaces
{
    public interface IProviderSourceDatasetsRepository
    {
        Task<IEnumerable<ProviderSourceDataset>> GetProviderSourceDatasetsByProviderIdAndSpecificationId(string providerId, string specificationId);
    }
}
