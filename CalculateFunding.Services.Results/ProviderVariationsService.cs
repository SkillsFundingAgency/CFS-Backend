using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Results;
using CalculateFunding.Models.Specs;
using CalculateFunding.Services.Results.Interfaces;
using Serilog;

namespace CalculateFunding.Services.Results
{
    public class ProviderVariationsService : IProviderVariationsService
    {
        private readonly IProviderVariationAssemblerService _providerVariationAssemblerService;
        private readonly ILogger _logger;

        public ProviderVariationsService(IProviderVariationAssemblerService providerVariationAssemblerService, ILogger logger)
        {
            Guard.ArgumentNotNull(providerVariationAssemblerService, nameof(providerVariationAssemblerService));
            Guard.ArgumentNotNull(logger, nameof(logger));

            _providerVariationAssemblerService = providerVariationAssemblerService;
            _logger = logger;
        }

        public async Task<IEnumerable<ProviderVariationError>> ProcessProviderVariations(JobViewModel triggeringJob, SpecificationCurrentVersion specification,
            IEnumerable<ProviderResult> providerResults, IEnumerable<PublishedProviderResultExisting> existingPublishedProviderResults,
            IEnumerable<PublishedProviderResult> allPublishedProviderResults, List<PublishedProviderResult> resultsToSave)
        {
            List<ProviderVariationError> errors = new List<ProviderVariationError>();

            // Only process on a refresh, not on choose
            if (existingPublishedProviderResults.Any())
            {
                _logger.Information($"Processing provider variations for specification '{specification.Id}' and job '{triggeringJob.Id}'");

                IEnumerable<ProviderChangeItem> providerVariations;
                try
                {
                    providerVariations = await _providerVariationAssemblerService.AssembleProviderVariationItems(providerResults, specification.Id);
                }
                catch (Exception ex)
                {
                    errors.Add(new ProviderVariationError { Error = ex.Message });
                    return errors;
                }

                foreach (ProviderChangeItem providerChange in providerVariations)
                {
                    foreach (AllocationLine allocationLine in specification.FundingStreams.SelectMany(f => f.AllocationLines))
                    {
                        (IEnumerable<ProviderVariationError> variationErrors, bool canContinue) processingResult = (Enumerable.Empty<ProviderVariationError>(), true);

                        if (providerChange.HasProviderClosed && providerChange.DoesProviderHaveSuccessor)
                        {
                            // If successor has a previous result
                            PublishedProviderResultExisting successorExistingResult = existingPublishedProviderResults.FirstOrDefault(r => r.ProviderId == providerChange.SuccessorProviderId
                                && (r.ProviderLookups != null
                                && r.ProviderLookups.Any(p => p.ProviderType == providerChange.UpdatedProvider.ProviderType && p.ProviderSubType == providerChange.UpdatedProvider.ProviderSubType)));

                            if (successorExistingResult != null)
                            {
                                processingResult = ProcessProviderClosedWithSuccessor(providerChange, allocationLine, specification, existingPublishedProviderResults, allPublishedProviderResults, resultsToSave, successorExistingResult);
                            }
                            else
                            {
                                _logger.Information($"Provider '{providerChange.UpdatedProvider.Id}' has closed and has successor but has no existing result. Specifiction '{specification.Id}' and allocation line {allocationLine.Id}");
                            }
                        }
                        else if (providerChange.HasProviderClosed && !providerChange.DoesProviderHaveSuccessor)
                        {
                            // Get previous result for affected provider
                            PublishedProviderResultExisting affectedProviderExistingResult = existingPublishedProviderResults.FirstOrDefault(r => r.ProviderId == providerChange.UpdatedProvider.Id
                                && r.AllocationLineId == allocationLine.Id);

                            if (affectedProviderExistingResult != null)
                            {
                                processingResult = ProcessProviderClosedWithoutSuccessor(providerChange, allocationLine, specification, affectedProviderExistingResult, allPublishedProviderResults, resultsToSave);
                            }
                            else
                            {
                                _logger.Information($"Provider '{providerChange.UpdatedProvider.Id}' has closed without successor but has no existing result. Specification '{specification.Id}' and allocation line '{allocationLine.Id}'");
                            }
                        }

                        errors.AddRange(processingResult.variationErrors);

                        if (!processingResult.canContinue)
                        {
                            continue;
                        }

                        if (providerChange.HasProviderDataChanged)
                        {
                            ProcessProviderDataChanged(providerChange, allocationLine, existingPublishedProviderResults, allPublishedProviderResults, resultsToSave);
                        }
                    }
                }
            }
            else
            {
                _logger.Information($"Not processing variations for specification '{specification.Id}' as job '{triggeringJob.Id}' is not for Refresh.");
            }

            return errors;
        }

        private (IEnumerable<ProviderVariationError> variationErrors, bool canContinue) ProcessProviderClosedWithSuccessor(
            ProviderChangeItem providerChange,
            AllocationLine allocationLine,
            SpecificationCurrentVersion specification,
            IEnumerable<PublishedProviderResultExisting> existingPublishedProviderResults,
            IEnumerable<PublishedProviderResult> allPublishedProviderResults,
            List<PublishedProviderResult> resultsToSave,
            PublishedProviderResultExisting successorExistingResult)
        {
            List<ProviderVariationError> errors = new List<ProviderVariationError>();

            _logger.Information($"Processing provider {providerChange.UpdatedProvider.Id} when closed with successor. Specification '{specification.Id}' and allocation line {allocationLine.Id}");

            // Get previous result for affected provider
            PublishedProviderResultExisting affectedProviderExistingResult = existingPublishedProviderResults.FirstOrDefault(r => r.ProviderId == providerChange.UpdatedProvider.Id
                && r.AllocationLineId == allocationLine.Id);

            if (affectedProviderExistingResult == null)
            {
                errors.Add(new ProviderVariationError { AllocationLineId = allocationLine.Id, Error = "Could not find existing result for affected provider", UKPRN = providerChange.UpdatedProvider.UKPRN });
                return (errors, false);
            }

            if (affectedProviderExistingResult.HasResultBeenVaried)
            {
                // Don't apply variation logic to an already varied result
                _logger.Information($"Result for provider {providerChange.UpdatedProvider.Id} and allocation line {allocationLine.Id} has already been varied. Specification '{specification.Id}'");
                return (errors, false);
            }

            // Find profiling periods after the variation date, for the affected provider
            IEnumerable<ProfilingPeriod> affectedProfilingPeriods = Enumerable.Empty<ProfilingPeriod>();
            if (!affectedProviderExistingResult.ProfilePeriods.IsNullOrEmpty())
            {
                affectedProfilingPeriods = affectedProviderExistingResult.ProfilePeriods.Where(p => p.PeriodDate > specification.VariationDate);
            }

            // See if the successor has already had a result generated that needs to be saved
            PublishedProviderResult successorResult = resultsToSave.FirstOrDefault(r => r.ProviderId == providerChange.SuccessorProviderId
                && (r.FundingStreamResult.AllocationLineResult.AllocationLine.ProviderLookups != null
                && r.FundingStreamResult.AllocationLineResult.AllocationLine.ProviderLookups.Any(p => p.ProviderType == providerChange.UpdatedProvider.ProviderType && p.ProviderSubType == providerChange.UpdatedProvider.ProviderSubType)));

            if (successorResult == null)
            {
                // If no new result for the successor so copy from the generated list as a base
                successorResult = allPublishedProviderResults.FirstOrDefault(r => r.ProviderId == providerChange.SuccessorProviderId
                    && r.FundingStreamResult.AllocationLineResult.AllocationLine.ProviderLookups.Any(p => p.ProviderType == providerChange.UpdatedProvider.ProviderType && p.ProviderSubType == providerChange.UpdatedProvider.ProviderSubType));

                if (successorResult == null)
                {
                    _logger.Information($"Could not find result for successor provider {providerChange.UpdatedProvider.Id} and allocation line {allocationLine.Id} to update. Specification '{specification.Id}'");
                    errors.Add(new ProviderVariationError { UKPRN = providerChange.UpdatedProvider.UKPRN, Error = "Could not find/create result for successor", AllocationLineId = allocationLine.Id });
                    return (errors, false);
                }

                _logger.Information($"Creating new result for successor provider {providerChange.UpdatedProvider.Id} and allocation line {allocationLine.Id}. Specification '{specification.Id}'");
                resultsToSave.Add(successorResult);
            }

            // Have to copy info from existing result otherwise they won't be set
            CopyPropertiesFromExisitngResult(successorResult, successorExistingResult);

            decimal affectedProfilingPeriodsTotal = affectedProfilingPeriods.Sum(p => p.Value);

            // Move the values from each affected profiling periods from the affected provider to the successor
            foreach (ProfilingPeriod profilePeriod in affectedProfilingPeriods)
            {
                ProfilingPeriod successorProfilePeriod = successorResult.FundingStreamResult.AllocationLineResult.Current.ProfilingPeriods.FirstOrDefault(p => p.Period == profilePeriod.Period && p.Year == profilePeriod.Year && p.Type == profilePeriod.Type);

                if (successorProfilePeriod == null)
                {
                    _logger.Information($"Creating new profile for successor provider {providerChange.SuccessorProviderId}. Specification '{specification.Id}' and allocation line {allocationLine.Id}");

                    successorProfilePeriod = new Models.Results.ProfilingPeriod
                    {
                        DistributionPeriod = profilePeriod.DistributionPeriod,
                        Occurrence = profilePeriod.Occurrence,
                        Period = profilePeriod.Period,
                        Type = profilePeriod.Type,
                        Year = profilePeriod.Year
                    };

                    List<ProfilingPeriod> tempPeriods = new List<ProfilingPeriod>(successorResult.FundingStreamResult.AllocationLineResult.Current.ProfilingPeriods);
                    tempPeriods.AddRange(new[] { successorProfilePeriod });
                    successorResult.FundingStreamResult.AllocationLineResult.Current.ProfilingPeriods = tempPeriods;
                }

                // Add the value from the affected profile to the matching successor profile
                successorProfilePeriod.Value += profilePeriod.Value;
            }

            // Add the amount in the affected profiling periods to the successors allocation total
            successorResult.FundingStreamResult.AllocationLineResult.Current.Value += affectedProfilingPeriodsTotal;

            // Set a flag to indicate successor result has been varied
            successorResult.FundingStreamResult.AllocationLineResult.HasResultBeenVaried = true;

            // See if the affected provider has already had a result generated that needs to be saved
            PublishedProviderResult affectedResult = resultsToSave.FirstOrDefault(r => r.ProviderId == providerChange.UpdatedProvider.Id
                && r.FundingStreamResult.AllocationLineResult.AllocationLine.Id == allocationLine.Id);

            if (affectedResult == null)
            {
                // No new result to save so copy the from the generated version to use as a base
                affectedResult = allPublishedProviderResults.FirstOrDefault(r => r.ProviderId == providerChange.UpdatedProvider.Id
                    && r.FundingStreamResult.AllocationLineResult.AllocationLine.Id == allocationLine.Id);

                if (affectedResult == null)
                {
                    errors.Add(new ProviderVariationError { AllocationLineId = allocationLine.Id, Error = "Could not find/create result for successor", UKPRN = providerChange.UpdatedProvider.UKPRN });
                    return (errors, false);
                }

                _logger.Information($"Creating new result for affected provider {providerChange.UpdatedProvider.Id} and allocation line {allocationLine.Id}. Specification '{specification.Id}'");
                resultsToSave.Add(affectedResult);
            }

            // Have to copy info from existing result otherwise they won't be set
            CopyPropertiesFromExisitngResult(affectedResult, affectedProviderExistingResult);

            // Zero out the affected profile periods in the affected provider
            foreach (ProfilingPeriod profilePeriod in affectedProfilingPeriods)
            {
                profilePeriod.Value = 0;
            }

            // Remove the amount in the affected profiling periods from the affected providers allocation total
            affectedResult.FundingStreamResult.AllocationLineResult.Current.Value -= affectedProfilingPeriodsTotal;

            // Set a flag to indicate result has been varied
            affectedResult.FundingStreamResult.AllocationLineResult.HasResultBeenVaried = true;

			// Concat the predecessor information to the successor
	        PublishedAllocationLineResultVersion currentPublishedAllocationLineResult = successorResult.FundingStreamResult.AllocationLineResult.Current;

	        if (currentPublishedAllocationLineResult.Predecessors == null)
	        {
		        currentPublishedAllocationLineResult.Predecessors = Enumerable.Empty<string>();
	        }

	        currentPublishedAllocationLineResult.Predecessors = currentPublishedAllocationLineResult.Predecessors.Concat(new []{providerChange.UpdatedProvider.UKPRN});

			return (errors, true);
        }

        private (IEnumerable<ProviderVariationError> variationErrors, bool canContinue) ProcessProviderClosedWithoutSuccessor(
            ProviderChangeItem providerChange,
            AllocationLine allocationLine,
            SpecificationCurrentVersion specification,
            PublishedProviderResultExisting affectedProviderExistingResult,
            IEnumerable<PublishedProviderResult> allPublishedProviderResults,
            List<PublishedProviderResult> resultsToSave)
        {
            List<ProviderVariationError> errors = new List<ProviderVariationError>();

            _logger.Information($"Processing provider {providerChange.UpdatedProvider.Id} when closed without successor. Specification '{specification.Id}' and allocation line {allocationLine.Id}");

            if (affectedProviderExistingResult.HasResultBeenVaried)
            {
                // Don't apply variation logic to an already varied result
                _logger.Information($"Result for provider {providerChange.UpdatedProvider.Id} and allocation line {allocationLine.Id} has already been varied. Specification '{specification.Id}'");
                return (errors, false);
            }

            // Find profiling periods after the variation date, for the affected provider
            IEnumerable<ProfilingPeriod> affectedProfilingPeriods = Enumerable.Empty<ProfilingPeriod>();
            if (!affectedProviderExistingResult.ProfilePeriods.IsNullOrEmpty())
            {
                affectedProfilingPeriods = affectedProviderExistingResult.ProfilePeriods.Where(p => p.PeriodDate > specification.VariationDate);
            }

            // See if the affected provider has already had a result generated that needs to be saved
            PublishedProviderResult affectedResult = resultsToSave.FirstOrDefault(r => r.ProviderId == providerChange.UpdatedProvider.Id
                && r.FundingStreamResult.AllocationLineResult.AllocationLine.Id == allocationLine.Id);

            if (affectedResult == null)
            {
                // No new result to save so copy the from the generated version to use as a base
                affectedResult = allPublishedProviderResults.FirstOrDefault(r => r.ProviderId == providerChange.UpdatedProvider.Id
                    && r.FundingStreamResult.AllocationLineResult.AllocationLine.Id == allocationLine.Id);

                if (affectedResult == null)
                {
                    errors.Add(new ProviderVariationError { AllocationLineId = allocationLine.Id, Error = "Could not find/create result for successor", UKPRN = providerChange.UpdatedProvider.UKPRN });
                    return (errors, false);
                }

                _logger.Information($"Creating new result for affected provider {providerChange.UpdatedProvider.Id} and allocation line {allocationLine.Id}. Specification '{specification.Id}'");
                resultsToSave.Add(affectedResult);
            }

            // Have to copy info from existing result otherwise they won't be set
            CopyPropertiesFromExisitngResult(affectedResult, affectedProviderExistingResult);

            decimal affectedProfilingPeriodsTotal = affectedProfilingPeriods.Sum(p => p.Value);

            // Zero out the affected profile periods in the affected provider
            foreach (ProfilingPeriod profilePeriod in affectedProfilingPeriods)
            {
                profilePeriod.Value = 0;
            }

            // Remove the amount in the affected profiling periods from the affected providers allocation total
            affectedResult.FundingStreamResult.AllocationLineResult.Current.Value -= affectedProfilingPeriodsTotal;

            // Set a flag to indicate result has been varied
            affectedResult.FundingStreamResult.AllocationLineResult.HasResultBeenVaried = true;

            return (errors, true);
        }

        private void ProcessProviderDataChanged(
            ProviderChangeItem providerChange, 
            AllocationLine allocationLine, 
            IEnumerable<PublishedProviderResultExisting> existingPublishedProviderResults,
            IEnumerable<PublishedProviderResult> allPublishedProviderResults,
            List<PublishedProviderResult> resultsToSave)
        {
            // See if the affected provider has already had a result generated that needs to be saved
            PublishedProviderResult affectedResult = resultsToSave.FirstOrDefault(r => r.ProviderId == providerChange.UpdatedProvider.Id
                && r.FundingStreamResult.AllocationLineResult.AllocationLine.Id == allocationLine.Id);

            if (affectedResult == null)
            {
                // Get previous result for affected provider
                PublishedProviderResultExisting affectedProviderExistingResult = existingPublishedProviderResults.FirstOrDefault(r => r.ProviderId == providerChange.UpdatedProvider.Id
                    && r.AllocationLineId == allocationLine.Id);

                if (affectedProviderExistingResult != null)
                {
                    // No new result to save so copy the from the generated version to use as a base
                    affectedResult = allPublishedProviderResults.FirstOrDefault(r => r.ProviderId == providerChange.UpdatedProvider.Id
                        && r.FundingStreamResult.AllocationLineResult.AllocationLine.Id == allocationLine.Id);
                    CopyPropertiesFromExisitngResult(affectedResult, affectedProviderExistingResult);

                    _logger.Information($"Creating new result for provider {providerChange.UpdatedProvider.Id} and allocation line {allocationLine.Id}. Specification '{affectedResult.SpecificationId}'");
                    resultsToSave.Add(affectedResult);
                }
            }

            // If still no result to save then there is nothing to update
            if (affectedResult != null)
            {
                _logger.Information($"Processing data update for provider {providerChange.UpdatedProvider.Id} and allocation line {allocationLine.Id}. Specification '{affectedResult.SpecificationId}'");

                affectedResult.FundingStreamResult.AllocationLineResult.Current.Provider = providerChange.UpdatedProvider;
                affectedResult.FundingStreamResult.AllocationLineResult.Current.VariationReasons = providerChange.VariationReasons;
            }
        }

        private void CopyPropertiesFromExisitngResult(PublishedProviderResult result, PublishedProviderResultExisting existingResult)
        {
            result.FundingStreamResult.AllocationLineResult.Current.Major = existingResult.Major;
            result.FundingStreamResult.AllocationLineResult.Current.Minor = existingResult.Minor;

            result.FundingStreamResult.AllocationLineResult.Current.ProfilingPeriods = existingResult.ProfilePeriods.ToArray();
            result.FundingStreamResult.AllocationLineResult.Current.FinancialEnvelopes = existingResult.FinancialEnvelopes;
        }
    }
}
