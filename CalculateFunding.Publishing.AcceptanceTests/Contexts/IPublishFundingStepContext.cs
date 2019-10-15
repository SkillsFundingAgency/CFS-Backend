using CalculateFunding.Common.ApiClient.Calcs.Models;
using CalculateFunding.Models.Publishing;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Publishing.AcceptanceTests.Contexts
{
    public interface IPublishFundingStepContext
    {
        bool PublishSuccessful { get; set; }

        bool RefreshSuccessful { get; set; }

        TemplateMapping TemplateMapping { get; set; }

        IEnumerable<CalculationResult> CalculationResults { get; set; }

        IEnumerable<CalculationMetadata> CalculationMetadata { get; set; }

        Task PublishFunding(string specificationId, string jobId, string userId, string userName);

        Task RefreshFunding(string specificationId, string jobId, string userId, string userName);
    }
}
