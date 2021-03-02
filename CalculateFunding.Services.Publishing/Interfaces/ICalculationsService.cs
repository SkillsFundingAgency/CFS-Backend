using CalculateFunding.Common.ApiClient.Calcs.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface ICalculationsService
    {
        Task<bool> HaveAllTemplateCalculationsBeenApproved(string specificationId);
        Task<TemplateMapping> GetTemplateMapping(string specificationId, string fundingStreamId);
        Task<IEnumerable<CalculationMetadata>> GetCalculationMetadataForSpecification(string specificationId);
        Task<IEnumerable<ObsoleteItem>> GetObsoleteItemsForSpecification(string specificationId);
    }
}
