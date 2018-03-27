using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.ServiceBus;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.EventHubs;

namespace CalculateFunding.Services.Specs.Interfaces
{
    public interface ISpecificationsService
    {
        Task<IActionResult> CreateSpecification(HttpRequest request);

        Task<IActionResult> GetSpecificationById(HttpRequest request);

        Task<IActionResult> GetSpecificationByAcademicYearId(HttpRequest request);

        Task<IActionResult> GetSpecificationByName(HttpRequest request);

        Task<IActionResult> GetAcademicYears(HttpRequest request);

        Task<IActionResult> GetFundingStreams(HttpRequest request);

        Task<IActionResult> GetAllocationLines(HttpRequest request);

        Task<IActionResult> GetPolicyByName(HttpRequest request);

        Task<IActionResult> CreatePolicy(HttpRequest request);

        Task<IActionResult> CreateCalculation(HttpRequest request);

        Task<IActionResult> GetCalculationByName(HttpRequest request);

        Task<IActionResult> GetCalculationBySpecificationIdAndCalculationId(HttpRequest request);

        Task<IActionResult> GetCalculationsBySpecificationId(HttpRequest request);

        Task AssignDataDefinitionRelationship(EventData message);

        Task<IActionResult> ReIndex();
    }
}
