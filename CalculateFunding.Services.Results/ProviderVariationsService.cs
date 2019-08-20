using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Policies;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Obsoleted;
using CalculateFunding.Models.Providers;
using CalculateFunding.Models.Results;
using CalculateFunding.Models.Specs;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Results.Interfaces;
using Newtonsoft.Json;
using Serilog;
using PolicyModels = CalculateFunding.Common.ApiClient.Policies.Models;

namespace CalculateFunding.Services.Results
{
    public class ProviderVariationsService : IProviderVariationsService
    {
        private readonly IProviderVariationAssemblerService _providerVariationAssemblerService;
        private readonly IPoliciesApiClient _policiesApiClient;
        private readonly Polly.Policy _policiesApiClientPolicy;
        private readonly ILogger _logger;
        private readonly IMapper _mapper;

        public ProviderVariationsService(
            IProviderVariationAssemblerService providerVariationAssemblerService,
            IPoliciesApiClient policiesApiClient,
            IResultsResiliencePolicies resiliencePolicies,
            ILogger logger,
            IMapper mapper)
        {
            Guard.ArgumentNotNull(providerVariationAssemblerService, nameof(providerVariationAssemblerService));
            Guard.ArgumentNotNull(policiesApiClient, nameof(policiesApiClient));
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(mapper, nameof(mapper));

            _providerVariationAssemblerService = providerVariationAssemblerService;
            _policiesApiClient = policiesApiClient;
            _policiesApiClientPolicy = resiliencePolicies.PoliciesApiClient;
            _logger = logger;
            _mapper = mapper;
        }

        public async Task<ProcessProviderVariationsResult> ProcessProviderVariations(
            JobViewModel triggeringJob,
            SpecificationCurrentVersion specification,
            IEnumerable<ProviderResult> providerResults,
            IEnumerable<PublishedProviderResultExisting> existingPublishedProviderResults,
            IEnumerable<PublishedProviderResult> allPublishedProviderResults,
            List<PublishedProviderResult> resultsToSave,
            Reference author)
        {
            Guard.ArgumentNotNull(triggeringJob, nameof(triggeringJob));
            Guard.ArgumentNotNull(specification, nameof(specification));
            Guard.ArgumentNotNull(providerResults, nameof(providerResults));
            Guard.ArgumentNotNull(existingPublishedProviderResults, nameof(existingPublishedProviderResults));
            Guard.ArgumentNotNull(allPublishedProviderResults, nameof(allPublishedProviderResults));
            Guard.ArgumentNotNull(resultsToSave, nameof(resultsToSave));
            Guard.ArgumentNotNull(author, nameof(author));

            List<ProviderVariationError> errors = new List<ProviderVariationError>();
            ProcessProviderVariationsResult result = new ProcessProviderVariationsResult();

            // Only process on a refresh, not on choose
            if (existingPublishedProviderResults.Any())
            {
                _logger.Information($"Processing provider variations for specification '{specification.Id}' and job '{triggeringJob.Id}'");

                IEnumerable<ProviderChangeItem> providerVariations;
                try
                {
                    providerVariations = await _providerVariationAssemblerService.AssembleProviderVariationItems(providerResults, existingPublishedProviderResults, specification.Id);

                    if (providerVariations.AnyWithNullCheck(v => v.HasProviderClosed) && !specification.VariationDate.HasValue)
                    {
                        errors.Add(new ProviderVariationError { Error = "Variations have been found for the scoped providers, but the specification has no variation date set" });
                        result.Errors = errors;
                        return result;
                    }

                    Period fundingPeriod = await GetFundingPeriod(specification);

                    foreach (ProviderChangeItem providerChange in providerVariations)
                    {
                        foreach (AllocationLine allocationLine in specification.FundingStreams.SelectMany(f => f.AllocationLines))
                        {
                            (IEnumerable<ProviderVariationError> variationErrors, bool canContinue) processingResult = (Enumerable.Empty<ProviderVariationError>(), true);

                            if (providerChange.HasProviderClosed && providerChange.DoesProviderHaveSuccessor)
                            {
                                // If successor has a previous result
                                PublishedProviderResultExisting successorExistingResult = null;

                                if (allocationLine.ProviderLookups.Any(p => p.ProviderType == providerChange.UpdatedProvider.ProviderType && p.ProviderSubType == providerChange.UpdatedProvider.ProviderSubType))
                                {
                                    successorExistingResult = existingPublishedProviderResults.FirstOrDefault(r => r.ProviderId == providerChange.SuccessorProviderId);
                                }

                                if (successorExistingResult != null)
                                {
                                    processingResult = ProcessProviderClosedWithSuccessor(providerChange, allocationLine, specification, existingPublishedProviderResults, allPublishedProviderResults, resultsToSave, successorExistingResult);
                                }
                                else
                                {
                                    processingResult = ProcessProviderClosedWithSuccessor(providerChange, allocationLine, specification, existingPublishedProviderResults, allPublishedProviderResults, resultsToSave, author, fundingPeriod);
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

                    if (!errors.Any())
                    {
                        result.ProviderChanges = providerVariations;
                    }
                }
                catch (Exception ex)
                {
                    errors.Add(new ProviderVariationError { Error = ex.Message });
                    result.Errors = errors;
                    return result;
                }
            }
            else
            {
                _logger.Information($"Not processing variations for specification '{specification.Id}' as job '{triggeringJob.Id}' is not for Refresh.");
            }

            result.Errors = errors;

            return result;
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
                _logger.Information($"No existing result for provider {providerChange.UpdatedProvider.Id} and allocation line {allocationLine.Id} to vary. Specification '{specification.Id}'");
                return (errors, true);
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

            if (affectedProfilingPeriods.IsNullOrEmpty())
            {
                _logger.Information($"There are no affected profiling periods for the allocation line result {allocationLine.Id} and provider {providerChange.UpdatedProvider.Id}");
                return (errors, true);
            }

            // See if the successor has already had a result generated that needs to be saved
            PublishedProviderResult successorResult = resultsToSave.FirstOrDefault(r => r.ProviderId == providerChange.SuccessorProviderId
                && (allocationLine.ProviderLookups != null
                && allocationLine.ProviderLookups.Any(p => p.ProviderType == providerChange.UpdatedProvider.ProviderType && p.ProviderSubType == providerChange.UpdatedProvider.ProviderSubType)));

            if (successorResult == null)
            {
                // If no new result for the successor so copy from the generated list as a base
                successorResult = allPublishedProviderResults.FirstOrDefault(r => r.ProviderId == providerChange.SuccessorProviderId
                    && (allocationLine.ProviderLookups != null
                    && allocationLine.ProviderLookups.Any(p => p.ProviderType == providerChange.UpdatedProvider.ProviderType && p.ProviderSubType == providerChange.UpdatedProvider.ProviderSubType)));

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

            EnsurePredecessors(successorResult, affectedProviderExistingResult.ProviderId);

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

            EnsureProviderUpToDate(affectedResult, providerChange);

            // Zero out the affected profile periods in the affected provider
            foreach (ProfilingPeriod profilePeriod in affectedProfilingPeriods)
            {
                profilePeriod.Value = 0;
            }

            // Remove the amount in the affected profiling periods from the affected providers allocation total
            affectedResult.FundingStreamResult.AllocationLineResult.Current.Value -= affectedProfilingPeriodsTotal;

            // Set a flag to indicate result has been varied
            affectedResult.FundingStreamResult.AllocationLineResult.HasResultBeenVaried = true;

            // Ensure the predecessor information is added to the successor
            EnsurePredecessors(successorResult, providerChange.UpdatedProvider.UKPRN);

            return (errors, true);
        }

        private (IEnumerable<ProviderVariationError> variationErrors, bool canContinue) ProcessProviderClosedWithSuccessor(
            ProviderChangeItem providerChange,
            AllocationLine allocationLine,
            SpecificationCurrentVersion specification,
            IEnumerable<PublishedProviderResultExisting> existingPublishedProviderResults,
            IEnumerable<PublishedProviderResult> allPublishedProviderResults,
            List<PublishedProviderResult> resultsToSave,
            Reference author,
            Period fundingPeriod)
        {
            List<ProviderVariationError> errors = new List<ProviderVariationError>();

            _logger.Information($"Processing Provider '{providerChange.UpdatedProvider.Id}' when closed with successor but has no existing result. Specifiction '{specification.Id}' and allocation line {allocationLine.Id}");

            // Get previous result for affected provider
            PublishedProviderResultExisting affectedProviderExistingResult = existingPublishedProviderResults.FirstOrDefault(r => r.ProviderId == providerChange.UpdatedProvider.Id
                && r.AllocationLineId == allocationLine.Id);

            if (affectedProviderExistingResult == null)
            {
                _logger.Information($"No existing result for provider {providerChange.UpdatedProvider.Id} and allocation line {allocationLine.Id} to vary. Specification '{specification.Id}'");
                return (errors, true);
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

            if (affectedProfilingPeriods.IsNullOrEmpty())
            {
                _logger.Information($"There are no affected profiling periods for the allocation line result {allocationLine.Id} and provider {providerChange.UpdatedProvider.Id}");
                return (errors, true);
            }

            // Check for an existing result for the successor
            IEnumerable<PublishedProviderResult> successorResults = resultsToSave.Where(r => r.ProviderId == providerChange.SuccessorProviderId);

            PublishedProviderResult successorResult = null;

            // Find existing result which is in the same funding stream as current allocation line (spec may have multiple) and which matches the provider type and subtype
            if (successorResults.Any())
            {
                foreach (PublishedProviderResult existingResult in successorResults)
                {
                    foreach (FundingStream fundingStream in specification.FundingStreams)
                    {
                        if (fundingStream.AllocationLines.Any(a => a.Id == allocationLine.Id))
                        {
                            foreach (AllocationLine fsAllocationLine in fundingStream.AllocationLines)
                            {
                                if (fsAllocationLine.ProviderLookups.AnyWithNullCheck(p => p.ProviderType == providerChange.SuccessorProvider.ProviderType && p.ProviderSubType == providerChange.SuccessorProvider.ProviderSubType))
                                {
                                    successorResult = existingResult;
                                    break;
                                }
                            }

                            if (successorResult != null) break;
                        }
                    }

                    if (successorResult != null) break;
                }
            }

            if (successorResult == null)
            {
                // If no new result for the successor then create one
                successorResult = CreateSuccessorResult(specification, providerChange, author, fundingPeriod);

                _logger.Information($"Creating new result for successor provider {providerChange.UpdatedProvider.Id} and allocation line {allocationLine.Id}. Specification '{specification.Id}'");
                resultsToSave.Add(successorResult);
            }

            // Copy info from existing result otherwise they won't be set
            successorResult.FundingStreamResult.AllocationLineResult.Current.ProfilingPeriods = MergeProfilingPeriods(successorResult.FundingStreamResult.AllocationLineResult.Current.ProfilingPeriods, affectedProviderExistingResult.ProfilePeriods);
            successorResult.FundingStreamResult.AllocationLineResult.Current.FinancialEnvelopes = MergeFinancialEnvelopes(successorResult.FundingStreamResult.AllocationLineResult.Current.FinancialEnvelopes, affectedProviderExistingResult.FinancialEnvelopes);

            decimal affectedProfilingPeriodsTotal = affectedProfilingPeriods.Sum(p => p.Value);

            // As we have moved all the periods from the affected result to the successor result we need to zero out the unaffected profile periods
            IEnumerable<ProfilingPeriod> unaffectedProfilingPeriods = affectedProviderExistingResult.ProfilePeriods.Except(affectedProfilingPeriods);

            foreach (ProfilingPeriod profilePeriod in unaffectedProfilingPeriods)
            {
                ProfilingPeriod successorProfilePeriod = successorResult.FundingStreamResult.AllocationLineResult.Current.ProfilingPeriods.FirstOrDefault(p => p.Period == profilePeriod.Period && p.Year == profilePeriod.Year && p.Type == profilePeriod.Type);

                // Zero the unaffected profile period
                successorProfilePeriod.Value = 0;
            }

            // Set the allocation total to the sum of the affected profiling periods
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
                    errors.Add(new ProviderVariationError { AllocationLineId = allocationLine.Id, Error = "Could not find/create result for affected provider", UKPRN = providerChange.UpdatedProvider.UKPRN });
                    return (errors, false);
                }

                _logger.Information($"Creating new result for affected provider {providerChange.UpdatedProvider.Id} and allocation line {allocationLine.Id}. Specification '{specification.Id}'");
                resultsToSave.Add(affectedResult);
            }

            // Have to copy info from existing result otherwise they won't be set
            CopyPropertiesFromExisitngResult(affectedResult, affectedProviderExistingResult);

            EnsureProviderUpToDate(affectedResult, providerChange);

            // Zero out the affected profile periods in the affected provider
            foreach (ProfilingPeriod profilePeriod in affectedProfilingPeriods)
            {
                profilePeriod.Value = 0;
            }

            // Remove the amount in the affected profiling periods from the affected providers allocation total
            affectedResult.FundingStreamResult.AllocationLineResult.Current.Value -= affectedProfilingPeriodsTotal;

            // Set a flag to indicate result has been varied
            affectedResult.FundingStreamResult.AllocationLineResult.HasResultBeenVaried = true;

            // Ensure the predecessor information is added to the successor
            EnsurePredecessors(successorResult, providerChange.UpdatedProvider.UKPRN);

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

            if (affectedProfilingPeriods.IsNullOrEmpty())
            {
                _logger.Information($"There are no affected profiling periods for the allocation line result {allocationLine.Id} and provider {providerChange.UpdatedProvider.Id}");
                return (errors, true);
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

            EnsureProviderUpToDate(affectedResult, providerChange);

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
            result.FundingStreamResult.AllocationLineResult.Current.Version = existingResult.Version + 1;

            result.FundingStreamResult.AllocationLineResult.Current.Major = existingResult.Major;
            result.FundingStreamResult.AllocationLineResult.Current.Minor = existingResult.Minor;

            result.FundingStreamResult.AllocationLineResult.Current.ProfilingPeriods = existingResult.ProfilePeriods.ToArray();
            result.FundingStreamResult.AllocationLineResult.Current.FinancialEnvelopes = existingResult.FinancialEnvelopes;

            if (existingResult.Status != AllocationLineStatus.Held)
            {
                result.FundingStreamResult.AllocationLineResult.Current.Status = AllocationLineStatus.Updated;
            }

            if (existingResult.Published != null)
            {
                result.FundingStreamResult.AllocationLineResult.Published = existingResult.Published;
            }
        }

        private void EnsureProviderUpToDate(PublishedProviderResult affectedResult, ProviderChangeItem providerChangeItem)
        {
            affectedResult.FundingStreamResult.AllocationLineResult.Current.Provider = providerChangeItem.UpdatedProvider;
        }

        private void EnsurePredecessors(PublishedProviderResult successorResult, string affectedProviderId)
        {
            if (successorResult.FundingStreamResult.AllocationLineResult.Current.Predecessors == null)
            {
                successorResult.FundingStreamResult.AllocationLineResult.Current.Predecessors = new List<string>();
            }

            if (!successorResult.FundingStreamResult.AllocationLineResult.Current.Predecessors.Contains(affectedProviderId))
            {
                ((List<string>)successorResult.FundingStreamResult.AllocationLineResult.Current.Predecessors).Add(affectedProviderId);
            }
        }

        private PublishedProviderResult CreateSuccessorResult(SpecificationCurrentVersion specification, ProviderChangeItem providerChangeItem, Reference author, Period fundingPeriod)
        {
            FundingStream fundingStream = GetFundingStream(specification, providerChangeItem);
            PublishedFundingStreamDefinition fundingStreamDefinition = _mapper.Map<PublishedFundingStreamDefinition>(fundingStream);

            AllocationLine allocationLine = fundingStream.AllocationLines
                .FirstOrDefault(a => a.ProviderLookups
                    .Any(l => l.ProviderType == providerChangeItem.SuccessorProvider.ProviderType
                              && l.ProviderSubType == providerChangeItem.SuccessorProvider.ProviderSubType));
            PublishedAllocationLineDefinition publishedAllocationLine = _mapper.Map<PublishedAllocationLineDefinition>(allocationLine);

            PublishedProviderResult successorResult = new PublishedProviderResult
            {
                FundingPeriod = fundingPeriod,
                FundingStreamResult = new PublishedFundingStreamResult
                {
                    AllocationLineResult = new PublishedAllocationLineResult
                    {
                        AllocationLine = publishedAllocationLine,
                        Current = new PublishedAllocationLineResultVersion
                        {
                            Author = author,
                            Calculations = null, // don't set calcs as result hasn't been generated from calculation run
                            Date = DateTimeOffset.Now,
                            Provider = providerChangeItem.SuccessorProvider,
                            ProviderId = providerChangeItem.SuccessorProviderId,
                            SpecificationId = specification.Id,
                            Status = AllocationLineStatus.Held,
                            Value = 0,
                            Version = 1
                        },
                        HasResultBeenVaried = true
                    },
                    DistributionPeriod = $"{fundingStream.PeriodType.Id}{specification.FundingPeriod.Id}",
                    FundingStream = fundingStreamDefinition,
                    FundingStreamPeriod = $"{fundingStream.Id}{specification.FundingPeriod.Id}"
                },
                ProviderId = providerChangeItem.SuccessorProviderId,
                SpecificationId = specification.Id,
            };

            successorResult.FundingStreamResult.AllocationLineResult.Current.PublishedProviderResultId = successorResult.Id;

            EnsurePredecessors(successorResult, providerChangeItem.UpdatedProvider.Id);

            return successorResult;
        }

        private static FundingStream GetFundingStream(SpecificationCurrentVersion specification, ProviderChangeItem providerChangeItem)
        {
            FundingStream fundingStream = specification.FundingStreams.FirstOrDefault(s => s.AllocationLines.Any(a => a.ProviderLookups.Any(l => l.ProviderType == providerChangeItem.SuccessorProvider.ProviderType && l.ProviderSubType == providerChangeItem.SuccessorProvider.ProviderSubType)));

            if (fundingStream == null)
            {
                throw new NonRetriableException($"Could not find funding stream with matching provider type '{providerChangeItem.SuccessorProvider.ProviderType}' and subtype '{providerChangeItem.SuccessorProvider.ProviderSubType}' for specification '{specification.Id}'");
            }

            return fundingStream;
        }

        private async Task<Period> GetFundingPeriod(SpecificationCurrentVersion specification)
        {
            ApiResponse<PolicyModels.Period> fundingPeriodResponse = await _policiesApiClientPolicy.ExecuteAsync(() => _policiesApiClient.GetFundingPeriodById(specification.FundingPeriod.Id));
            Period fundingPeriod = _mapper.Map<Period>(fundingPeriodResponse?.Content);

            if (fundingPeriod == null)
            {
                throw new NonRetriableException($"Failed to find a funding period for id: {specification.FundingPeriod.Id}");
            }

            return fundingPeriod;
        }

        private IEnumerable<ProfilingPeriod> MergeProfilingPeriods(IEnumerable<ProfilingPeriod> lhs, IEnumerable<ProfilingPeriod> rhs)
        {
            if (lhs.IsNullOrEmpty())
            {
                return DuplicateEnumerable(rhs);
            }

            List<ProfilingPeriod> list = new List<ProfilingPeriod>(lhs);
            JsonSerializerSettings deserialiseSettings = new JsonSerializerSettings { ObjectCreationHandling = ObjectCreationHandling.Replace };

            foreach (ProfilingPeriod item in rhs)
            {
                ProfilingPeriod foundItem = lhs.FirstOrDefault(p => p.PeriodDate == item.PeriodDate && p.Type == item.Type && p.DistributionPeriod == item.DistributionPeriod);

                if (foundItem == null)
                {
                    ProfilingPeriod clone = JsonConvert.DeserializeObject<ProfilingPeriod>(JsonConvert.SerializeObject(item), deserialiseSettings);

                    list.Add(clone);
                }
                else
                {
                    foundItem.Value += item.Value;
                }
            }

            return list;
        }

        private IEnumerable<FinancialEnvelope> MergeFinancialEnvelopes(IEnumerable<FinancialEnvelope> lhs, IEnumerable<FinancialEnvelope> rhs)
        {
            if (lhs.IsNullOrEmpty())
            {
                return DuplicateEnumerable(rhs);
            }

            List<FinancialEnvelope> list = new List<FinancialEnvelope>(lhs);
            JsonSerializerSettings deserialiseSettings = new JsonSerializerSettings { ObjectCreationHandling = ObjectCreationHandling.Replace };

            foreach (FinancialEnvelope item in rhs)
            {
                FinancialEnvelope foundItem = lhs.FirstOrDefault(e => e.StartDate == item.StartDate && e.EndDate == item.EndDate);

                if (foundItem == null)
                {
                    FinancialEnvelope clone = JsonConvert.DeserializeObject<FinancialEnvelope>(JsonConvert.SerializeObject(item), deserialiseSettings);

                    list.Add(clone);
                }
                else
                {
                    foundItem.Value += item.Value;
                }
            }

            return list;
        }

        private IEnumerable<T> DuplicateEnumerable<T>(IEnumerable<T> source)
        {
            if (source == null)
            {
                return null;
            }

            List<T> list = new List<T>();
            JsonSerializerSettings deserialiseSettings = new JsonSerializerSettings { ObjectCreationHandling = ObjectCreationHandling.Replace };

            foreach (T item in source)
            {
                T clone = JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(item), deserialiseSettings);

                list.Add(clone);
            }

            return list;
        }
    }
}
