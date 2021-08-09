using CalculateFunding.Common.ApiClient.Calcs.Models;
using CalculateFunding.Common.ApiClient.Calcs.Models.ObsoleteItems;
using CalculateFunding.Services.Publishing.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Publishing.AcceptanceTests.Repositories
{
    public class InMemoryCalculationsService : ICalculationsService
    {
        private readonly Dictionary<string, IEnumerable<ObsoleteItem>> _obsoleteItems = new Dictionary<string, IEnumerable<ObsoleteItem>>();

        public Task<IEnumerable<CalculationMetadata>> GetCalculationMetadataForSpecification(string specificationId)
        {
            throw new System.NotImplementedException();
        }

        public Task<IEnumerable<ObsoleteItem>> GetObsoleteItemsForSpecification(string specificationId)
        {
            _obsoleteItems.TryGetValue(specificationId, out IEnumerable<ObsoleteItem> obsoleteItems);

            return Task.FromResult(obsoleteItems);
        }

        public Task<TemplateMapping> GetTemplateMapping(string specificationId, string fundingStreamId)
        {
            throw new System.NotImplementedException();
        }

        public Task<bool> HaveAllTemplateCalculationsBeenApproved(string specificationId)
        {
            throw new System.NotImplementedException();
        }
    }
}
