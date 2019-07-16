﻿using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.ServiceBus;

namespace CalculateFunding.Services.Specs.Interfaces
{
    public interface ISpecificationsService
    {
        Task<IActionResult> CreateSpecification(HttpRequest request);

        Task<IActionResult> GetSpecifications(HttpRequest request);

        Task<IActionResult> GetSpecificationsSelectedForFunding(HttpRequest request);

        Task<IActionResult> GetSpecificationsSelectedForFundingByPeriod(HttpRequest request);

        Task<IActionResult> GetFundingStreamsSelectedForFundingBySpecification(HttpRequest request);

        Task<IActionResult> GetSpecificationSummaries(HttpRequest request);

        Task<IActionResult> EditCalculation(HttpRequest request);

        Task<IActionResult> GetSpecificationById(HttpRequest request);

        Task<IActionResult> GetSpecificationsByFundingPeriodId(HttpRequest request);

        Task<IActionResult> GetSpecificationByName(HttpRequest request);

        Task<IActionResult> GetSpecificationSummaryById(HttpRequest request);

        Task<IActionResult> GetSpecificationSummariesByIds(HttpRequest request);

        Task<IActionResult> GetCurrentSpecificationById(HttpRequest request);

        Task<IActionResult> GetFundingStreamsForSpecificationById(HttpRequest request);

        Task<IActionResult> CreateCalculation(HttpRequest request);

        Task<IActionResult> GetCalculationByName(HttpRequest request);

        Task<IActionResult> GetCalculationBySpecificationIdAndCalculationId(HttpRequest request);

        Task<IActionResult> GetCalculationsBySpecificationId(HttpRequest request);

        Task<IActionResult> GetBaselineCalculations(string specificationId, HttpRequest request);

        Task AssignDataDefinitionRelationship(Message message);

        Task<IActionResult> ReIndex();

        Task<IActionResult> EditSpecification(HttpRequest request);

        Task<IActionResult> EditSpecificationStatus(HttpRequest request);

        Task<IActionResult> GetCurrentSpecificationsByFundingPeriodIdAndFundingStreamId(HttpRequest request);

        Task<IActionResult> SelectSpecificationForFunding(HttpRequest request);

        Task<IActionResult> RefreshPublishedResults(HttpRequest request);

        Task<IActionResult> CheckPublishResultStatus(HttpRequest request);

        Task<IActionResult> UpdatePublishedRefreshedDate(HttpRequest request);

        Task<IActionResult> UpdateCalculationLastUpdatedDate(HttpRequest request);
    }
}
