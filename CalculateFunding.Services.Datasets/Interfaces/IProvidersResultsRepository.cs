using CalculateFunding.Models.Results;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Datasets.Interfaces
{
    public interface IProvidersResultsRepository
    {
        Task UpdateSourceDatsets(IEnumerable<ProviderSourceDataset> providerSourceDatasets);
    }
}
