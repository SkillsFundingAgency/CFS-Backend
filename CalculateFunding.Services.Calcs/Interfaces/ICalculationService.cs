﻿using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Aggregations;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Code;
using CalculateFunding.Models.Datasets.ViewModels;
using CalculateFunding.Models.Versioning;
using CalculateFunding.Services.Processing.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.ServiceBus;
using SpecModel = CalculateFunding.Common.ApiClient.Specifications.Models;

namespace CalculateFunding.Services.Calcs.Interfaces
{
    public interface ICalculationService : IProcessingService
    {
        Task<IActionResult> GetCalculationById(string calculationId);

        Task<IActionResult> GetCurrentCalculationsForSpecification(string specificationId);

        Task<IActionResult> GetCalculationSummariesForSpecification(string specificationId);

        Task<IActionResult> GetCalculationVersions(CalculationVersionsCompareModel calculationVersionsCompareModel);

        Task<IActionResult> GetCalculationHistory(string specificationId);

        Task<IActionResult> EditCalculation(string specificationId,
            string calculationId,
            CalculationEditModel calculationEditModel,
            Reference author,
            string correlationId,
            bool setAdditional = false,
            bool skipInstruct = false,
            bool skipValidation = false,
            bool updateBuildProject = true,
            bool setTemplate = false,
            CalculationEditMode calculationEditMode = CalculationEditMode.User,
            Calculation existingCalculation = null);

        Task<IActionResult> UpdateCalculationStatus(string calculationId, EditStatusModel editStatusModel);

        Task<ActionResult<IEnumerable<TypeInformation>>> GetCalculationCodeContext(string specificationId);

        Task<IActionResult> ReIndex();

        Task<IActionResult> GetCalculationStatusCounts(SpecificationListModel specifications);

        Task<IActionResult> IsCalculationNameValid(string specificationId, string calculationName, string existingCalculationId);

        Task ResetCalculationForFieldDefinitionChanges(IEnumerable<DatasetSpecificationRelationshipViewModel> relationships, string specificationId, IEnumerable<string> currentFieldDefinitionNames);

        Task<IActionResult> GetCalculationByName(CalculationGetModel model);

        Task<IActionResult> CreateAdditionalCalculation(string specificationId, CalculationCreateModel model, Reference author, string correlationId);

        Task<IActionResult> GetCalculationsMetadataForSpecification(string specificationId);

        Task<IActionResult> ProcessTemplateMappings(string specificationId, string templateVersion, string fundingStreamId);

        Task<IActionResult> GetMappedCalculationsOfSpecificationTemplate(string specificationId, string fundingStreamId);

        Task<IActionResult> CheckHasAllApprovedTemplateCalculationsForSpecificationId(string specificationId);

        Task DeleteCalculations(Message message);

        Task<IEnumerable<Calculation>> UpdateCalculationCodeOnCalculationChange(CalculationVersionComparisonModel comparison, Reference user);

        Task<IActionResult> UpdateTemplateCalculationsForSpecification(string specificationId, string datasetDefinitionId, Reference user);

        Task<BuildProject> UpdateBuildProject(SpecModel.SpecificationSummary specificationSummary);

        Task<IActionResult> QueueApproveAllSpecificationCalculations(string specificationId, Reference author, string correlationId);
    }
}
