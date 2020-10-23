using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Models;

namespace CalculateFunding.Services.Publishing.Specifications
{
    public interface IPublishedProviderFundingCsvDataProcessor
    {
        Task<IEnumerable<PublishedProviderFundingCsvData>> GetFundingData(IEnumerable<string> publishedProviderIds,
            string specificationId,
            params PublishedProviderStatus[] statuses);
    }
}