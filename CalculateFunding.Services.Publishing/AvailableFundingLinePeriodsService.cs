using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Policies;
using CalculateFunding.Common.ApiClient.Profiling;
using CalculateFunding.Common.ApiClient.Profiling.Models;
using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Profiling;
using Microsoft.AspNetCore.Mvc;
using Polly;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using ApiFundingLineType = CalculateFunding.Common.TemplateMetadata.Enums.FundingLineType;

namespace CalculateFunding.Services.Publishing
{
    public class AvailableFundingLinePeriodsService : IAvailableFundingLinePeriodsService
    {
        private readonly ISpecificationsApiClient _specsClient;
        private readonly IProfilingApiClient _profilingClient;
        private readonly IPoliciesApiClient _policiesClient;
        private readonly AsyncPolicy _specsPolicy;
        private readonly AsyncPolicy _profilingPolicy;
        private readonly AsyncPolicy _policiesPolicy;

        public AvailableFundingLinePeriodsService(
            ISpecificationsApiClient specificationsApiClient,
            IProfilingApiClient profilingApiClient,
            IPoliciesApiClient policiesApiClient,
            IPublishingResiliencePolicies publishingResiliencePolicies)
        {
            Guard.ArgumentNotNull(specificationsApiClient, nameof(specificationsApiClient));
            Guard.ArgumentNotNull(profilingApiClient, nameof(profilingApiClient));
            Guard.ArgumentNotNull(policiesApiClient, nameof(policiesApiClient));

            Guard.ArgumentNotNull(publishingResiliencePolicies, nameof(publishingResiliencePolicies));
            Guard.ArgumentNotNull(publishingResiliencePolicies.SpecificationsApiClient, nameof(publishingResiliencePolicies.SpecificationsApiClient));
            Guard.ArgumentNotNull(publishingResiliencePolicies.ProfilingApiClient, nameof(publishingResiliencePolicies.ProfilingApiClient));
            Guard.ArgumentNotNull(publishingResiliencePolicies.PoliciesApiClient, nameof(publishingResiliencePolicies.PoliciesApiClient));

            _specsClient = specificationsApiClient;
            _profilingClient = profilingApiClient;
            _policiesClient = policiesApiClient;

            _specsPolicy = publishingResiliencePolicies.SpecificationsApiClient;
            _profilingPolicy = publishingResiliencePolicies.ProfilingApiClient;
            _policiesPolicy = publishingResiliencePolicies.PoliciesApiClient;

        }

        public async Task<ActionResult<IEnumerable<AvailableVariationPointerFundingLine>>> GetAvailableFundingLineProfilePeriodsForVariationPointers(string specificationId)
        {
            ApiResponse<Common.ApiClient.Specifications.Models.SpecificationSummary> specsRequest = await _specsPolicy.ExecuteAsync(() => _specsClient.GetSpecificationSummaryById(specificationId));
            ActionResult error = specsRequest.IsSuccessOrReturnFailureResultAction("Specification");
            if (error != null)
            {
                return error;
            }

            Common.ApiClient.Specifications.Models.SpecificationSummary specification = specsRequest.Content;

            string fundingStreamId = specsRequest.Content.FundingStreams.First().Id;

            Task<ApiResponse<Common.ApiClient.Policies.Models.TemplateMetadataDistinctFundingLinesContents>> fundingLinesRequest = _policiesPolicy.ExecuteAsync(() => _policiesClient.GetDistinctTemplateMetadataFundingLinesContents(fundingStreamId, specification.FundingPeriod.Id, specification.TemplateIds[fundingStreamId]));
            Task<ApiResponse<IEnumerable<FundingStreamPeriodProfilePattern>>> profilingConfigRequest = _profilingPolicy.ExecuteAsync(() => _profilingClient.GetProfilePatternsForFundingStreamAndFundingPeriod(fundingStreamId, specification.FundingPeriod.Id));
            Task<ApiResponse<IEnumerable<Common.ApiClient.Specifications.Models.ProfileVariationPointer>>> currentVariationPointersRequest = _specsPolicy.ExecuteAsync(() => _specsClient.GetProfileVariationPointers(specificationId));

            await Task.WhenAll(fundingLinesRequest, profilingConfigRequest, currentVariationPointersRequest);

            error = fundingLinesRequest.Result.IsSuccessOrReturnFailureResultAction("Distict funding lines");
            if (error != null)
            {
                return error;
            }

            error = profilingConfigRequest.Result.IsSuccessOrReturnFailureResultAction("Profiling configuration");
            if (error != null)
            {
                return error;
            }

            if (currentVariationPointersRequest.Result.StatusCode != HttpStatusCode.NoContent)
            {
                error = currentVariationPointersRequest.Result.IsSuccessOrReturnFailureResultAction("Variation pointers");
                if (error != null)
                {
                    return error;
                }
            }

            IOrderedEnumerable<Common.ApiClient.Policies.Models.TemplateMetadataFundingLine> sortedPaymentFundingLines = fundingLinesRequest.Result.Content
                .FundingLines
                .Where(_ => _.Type == ApiFundingLineType.Payment)
                .OrderBy(_ => _.Name);

            IEnumerable<FundingStreamPeriodProfilePattern> profilingConfigs = profilingConfigRequest.Result.Content;

            List<AvailableVariationPointerFundingLine> results = new List<AvailableVariationPointerFundingLine>(sortedPaymentFundingLines.Count());

            foreach (Common.ApiClient.Policies.Models.TemplateMetadataFundingLine fundingLine in sortedPaymentFundingLines)
            {
                Common.ApiClient.Specifications.Models.ProfileVariationPointer existingPointer = currentVariationPointersRequest.Result.Content?.FirstOrDefault(_ => _.FundingLineId == fundingLine.FundingLineCode);

                AvailableVariationPointerProfilePeriod selectedVariationPointer = null;
                if (existingPointer != null)
                {
                    selectedVariationPointer = new AvailableVariationPointerProfilePeriod()
                    {
                        Occurrence = existingPointer.Occurrence,
                        Period = existingPointer.TypeValue,
                        PeriodType = existingPointer.PeriodType,
                        Year = existingPointer.Year,
                    };
                }

                List<ProfilePeriodPattern> allPatternsForFundingLine = new List<ProfilePeriodPattern>();

                foreach (FundingStreamPeriodProfilePattern profilingConfig in profilingConfigs.Where(_ => _.FundingLineId == fundingLine.FundingLineCode))
                {
                    allPatternsForFundingLine.AddRange(profilingConfig.ProfilePattern);
                }

                IEnumerable<ProfilePeriodPattern> distinctPeriodsForFundingLine = allPatternsForFundingLine.DistinctBy(_ => new
                {
                    _.Occurrence,
                    _.Period,
                    _.PeriodType,
                    _.PeriodYear
                });

                IEnumerable<ProfilePeriodPattern> yearMonthOrderedProfilePeriodPatterns = new YearMonthOrderedProfilePeriodPatterns(distinctPeriodsForFundingLine);

                yearMonthOrderedProfilePeriodPatterns = RemovePastPaidPeriodsFromSelection(selectedVariationPointer, yearMonthOrderedProfilePeriodPatterns);

                List<AvailableVariationPointerProfilePeriod> periods = new List<AvailableVariationPointerProfilePeriod>(yearMonthOrderedProfilePeriodPatterns.Count());

                AvailableVariationPointerFundingLine result = new AvailableVariationPointerFundingLine()
                {
                    FundingLineCode = fundingLine.FundingLineCode,
                    FundingLineName = fundingLine.Name,
                    SelectedPeriod = selectedVariationPointer,
                };

                foreach (ProfilePeriodPattern profilePeriod in yearMonthOrderedProfilePeriodPatterns)
                {
                    periods.Add(new AvailableVariationPointerProfilePeriod()
                    {
                        Occurrence = profilePeriod.Occurrence,
                        Period = profilePeriod.Period,
                        PeriodType = profilePeriod.PeriodType.ToString(),
                        Year = profilePeriod.PeriodYear,
                    });
                }

                result.Periods = periods;

                results.Add(result);
            }

            return results;
        }

        private static IEnumerable<ProfilePeriodPattern> RemovePastPaidPeriodsFromSelection(AvailableVariationPointerProfilePeriod selectedVariationPointer, IEnumerable<ProfilePeriodPattern> yearMonthOrderedProfilePeriodPatterns)
        {
            int currentPeriodIndex = -1;
            if (selectedVariationPointer != null)
            {
                currentPeriodIndex = yearMonthOrderedProfilePeriodPatterns.IndexOf(
                  _ => _.Occurrence == selectedVariationPointer.Occurrence
                  && _.Period == selectedVariationPointer.Period
                  && _.PeriodType == PeriodType.CalendarMonth
                  && _.PeriodYear == selectedVariationPointer.Year);
            }


            if (currentPeriodIndex > 0)
            {
                yearMonthOrderedProfilePeriodPatterns = yearMonthOrderedProfilePeriodPatterns.Skip(currentPeriodIndex);
            }

            return yearMonthOrderedProfilePeriodPatterns;
        }
    }
}
