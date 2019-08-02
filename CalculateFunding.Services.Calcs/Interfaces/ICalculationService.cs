﻿using CalculateFunding.Common.Models;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Datasets.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.ServiceBus;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Calcs.Interfaces
{
    public interface ICalculationService
    {
        Task<IActionResult> GetCalculationById(HttpRequest request);

        Task<IActionResult> GetCurrentCalculationsForSpecification(HttpRequest request);

        Task<IActionResult> GetCalculationSummariesForSpecification(HttpRequest request);

        Task<IActionResult> GetCalculationVersions(HttpRequest request);

        Task<IActionResult> GetCalculationHistory(HttpRequest request);

        Task<IActionResult> GetCalculationCurrentVersion(HttpRequest request);

        Task<IActionResult> SaveCalculationVersion(HttpRequest request);

        Task<IActionResult> UpdateCalculationStatus(HttpRequest request);

        Task<IActionResult> GetCalculationCodeContext(HttpRequest request);

        Task<IActionResult> ReIndex();

        Task UpdateCalculationsForSpecification(Message message);

        Task<IActionResult> GetCalculationStatusCounts(HttpRequest request);

        Task<IActionResult> IsCalculationNameValid(string specificationId, string calculationName, string existingCalculationId);

        Task<IActionResult> DuplicateCalcNamesMigration();

        Task ResetCalculationForFieldDefinitionChanges(IEnumerable<DatasetSpecificationRelationshipViewModel> relationships, string specificationId, IEnumerable<string> currentFieldDefinitionNames);

        Task<IActionResult> GetCalculationByName(CalculationGetModel model);

        Task<IActionResult> CreateAdditionalCalculation(string specificationId, CalculationCreateModel model, Reference author);
    }
}
