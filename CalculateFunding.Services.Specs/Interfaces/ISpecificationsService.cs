using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Specs;
using CalculateFunding.Models.Versioning;
using CalculateFunding.Services.Processing.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.ServiceBus;

namespace CalculateFunding.Services.Specs.Interfaces
{
    public interface ISpecificationsService : IJobProcessingService
    {
        Task<IActionResult> CreateSpecification(SpecificationCreateModel specificationCreateModel, Reference user, string correlationId);

        Task<IActionResult> GetSpecifications();

        Task<IActionResult> GetSpecificationsSelectedForFunding();

        Task<IActionResult> GetSpecificationsSelectedForFundingByPeriod(string fundingPeriodId);

        Task<IActionResult> GetFundingStreamsSelectedForFundingBySpecification(string specificationId);

        Task<IActionResult> GetSpecificationSummaries();

        Task<IActionResult> GetSpecificationById(string specificationId);

        Task<IActionResult> GetSpecificationsByFundingPeriodId(string fundingPeriodId);

        Task<IActionResult> GetSpecificationByName(string specificationName);

        Task<IActionResult> GetSpecificationSummaryById(string specificationId);

        Task<IActionResult> GetSpecificationSummariesByIds(string[] specificationIds);

        Task<IActionResult> ReIndex();

        Task<IActionResult> EditSpecification(string specificationId, SpecificationEditModel specificationEditModel, Reference user, string correlationId);

        Task<IActionResult> EditSpecificationStatus(string specificationId, EditStatusModel specificationEditModel, Reference user);

        Task<IActionResult> GetCurrentSpecificationsByFundingPeriodIdAndFundingStreamId(string fundingPeriodId, string fundingStreamId);

        Task<IActionResult> GetSpecificationWithResultsByFundingPeriodIdAndFundingStreamId(string fundingPeriodId, string fundingStreamId);
        
        Task<IActionResult> GetApprovedSpecificationsByFundingPeriodIdAndFundingStreamId(string fundingPeriodId, string fundingStreamId);

        Task<IActionResult> GetSelectedSpecificationsByFundingPeriodIdAndFundingStreamId(string fundingPeriodId, string fundingStreamId);

        Task<IActionResult> SelectSpecificationForFunding(string specificationId);

        Task<IActionResult> GetProfileVariationPointers(string specificationId);

        Task<IActionResult> SetProfileVariationPointers(string specificationId, IEnumerable<SpecificationProfileVariationPointerModel> specificationProfileVariationPointerModels, bool merge = false);

        Task<IActionResult> SetProfileVariationPointer(string specificationId, SpecificationProfileVariationPointerModel specificationProfileVariationPointerModel);

        Task<IActionResult> ClearForceUpdateOnNextRefresh(string specificationId);

        Task<IActionResult> GetFundingStreamIdsForSelectedFundingSpecifications();
        Task<IActionResult> GetFundingPeriodsByFundingStreamIdsForSelectedSpecifications(string fundingStreamId);
        Task<IActionResult> SoftDeleteSpecificationById(string specificationId, Reference user, string correlationId);
        Task<IActionResult> PermanentDeleteSpecificationById(string specificationId, Reference user, string correlationId, bool allowDelete = false);
        Task DeleteSpecification(Message message);
        Task<IActionResult> DeselectSpecificationForFunding(string specificationId);
        Task<IActionResult> GetDistinctFundingStreamsForSpecifications();
        Task<IActionResult> SetProviderVersion(AssignSpecificationProviderVersionModel assignSpecificationProviderVersionModel, Reference user);
        Task<IActionResult> GetSpecificationsWithProviderVersionUpdatesAsUseLatest();
    }
}
