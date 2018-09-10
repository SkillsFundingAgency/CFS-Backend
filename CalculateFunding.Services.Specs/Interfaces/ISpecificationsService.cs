using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.ServiceBus;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Specs.Interfaces
{
    public interface ISpecificationsService
    {
        Task<IActionResult> CreateSpecification(HttpRequest request);

        Task<IActionResult> GetSpecifications(HttpRequest request);

        Task<IActionResult> GetSpecificationsSelectedForFunding(HttpRequest request);

        Task<IActionResult> GetSpecificationSummaries(HttpRequest request);

        Task<IActionResult> EditCalculation(HttpRequest request);

        Task<IActionResult> GetSpecificationById(HttpRequest request);

        Task<IActionResult> GetSpecificationsByFundingPeriodId(HttpRequest request);
        Task<IActionResult> GetSpecificationByName(HttpRequest request);

        Task<IActionResult> GetSpecificationSummaryById(HttpRequest request);

        Task<IActionResult> GetSpecificationSummariesByIds(HttpRequest request);

        Task<IActionResult> GetCurrentSpecificationById(HttpRequest request);

        Task<IActionResult> GetFundingPeriods(HttpRequest request);

        Task<IActionResult> GetFundingStreams(HttpRequest request);

        Task<IActionResult> GetFundingStreamsForSpecificationById(HttpRequest request);

        Task<IActionResult> GetFundingStreamById(HttpRequest request);

        Task<IActionResult> GetPolicyByName(HttpRequest request);

        Task<IActionResult> CreatePolicy(HttpRequest request);

        Task<IActionResult> CreateCalculation(HttpRequest request);

        Task<IActionResult> GetCalculationByName(HttpRequest request);

        Task<IActionResult> GetCalculationBySpecificationIdAndCalculationId(HttpRequest request);

        Task<IActionResult> GetCalculationsBySpecificationId(HttpRequest request);

        Task AssignDataDefinitionRelationship(Message message);

        Task<IActionResult> ReIndex();

        Task<IActionResult> SaveFundingStream(HttpRequest request);

        Task<IActionResult> SaveFundingPeriods(HttpRequest request);

        Task<IActionResult> EditSpecification(HttpRequest request);

        Task<IActionResult> EditPolicy(HttpRequest request);

        Task<IActionResult> EditSpecificationStatus(HttpRequest request);

        Task<IActionResult> GetCurrentSpecificationsByFundingPeriodIdAndFundingStreamId(HttpRequest request);

        Task<IActionResult> SelectSpecificationForFunding(HttpRequest request);

        Task<IActionResult> GetFundingPeriodById(HttpRequest request);

        Task<IActionResult> CheckCalculationProgressForSpecifications(HttpRequest request);
    }
}
