using CalculateFunding.Common.ApiClient.Calcs;
using CalculateFunding.Common.ApiClient.Calcs.Models;
using CalculateFunding.Common.ApiClient.Calcs.Models.ObsoleteItems;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Services.Publishing.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Publishing.AcceptanceTests.Repositories
{
    public class InMemoryCalculationsService : ICalculationsService
    {
        private readonly Dictionary<string, IEnumerable<ObsoleteItem>> _obsoleteItems = new Dictionary<string, IEnumerable<ObsoleteItem>>();
        private readonly ICalculationsApiClient _calcsClient;

        public InMemoryCalculationsService(ICalculationsApiClient calculationsApiClient)
        {
            _calcsClient = calculationsApiClient;
        }

        public Task<IEnumerable<CalculationMetadata>> GetCalculationMetadataForSpecification(string specificationId)
        {
            throw new System.NotImplementedException();
        }

        public Task<IEnumerable<ObsoleteItem>> GetObsoleteItemsForSpecification(string specificationId)
        {
            _obsoleteItems.TryGetValue(specificationId, out IEnumerable<ObsoleteItem> obsoleteItems);

            return Task.FromResult(obsoleteItems);
        }

        public async Task<TemplateMapping> GetTemplateMapping(string specificationId, string fundingStreamId)
        {
            ApiResponse<TemplateMapping> response = await _calcsClient.GetTemplateMapping(specificationId, fundingStreamId);
            if (response == null || response.Content == null || response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                return null;
            }

            return response.Content;
        }

        public Task<bool> HaveAllTemplateCalculationsBeenApproved(string specificationId)
        {
            throw new System.NotImplementedException();
        }
    }
}
