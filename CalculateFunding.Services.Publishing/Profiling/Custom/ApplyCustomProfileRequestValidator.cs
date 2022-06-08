using System;
using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Common.ApiClient.Policies.Models.FundingConfig;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Publishing.Interfaces;
using FluentValidation;

namespace CalculateFunding.Services.Publishing.Profiling.Custom
{
    public class ApplyCustomProfileRequestValidator : AbstractValidator<ApplyCustomProfileRequest>
    {
        public ApplyCustomProfileRequestValidator(
            IJobsRunning jobsRunning,
            IPublishedFundingRepository publishedFunding,
            IPublishingResiliencePolicies resiliencePolicies,
            IPoliciesService policiesService)
        {
            Guard.ArgumentNotNull(publishedFunding, nameof(publishedFunding));
            Guard.ArgumentNotNull(resiliencePolicies?.PublishedFundingRepository, nameof(resiliencePolicies.PublishedFundingRepository));
            Guard.ArgumentNotNull(policiesService, nameof(policiesService));
            Guard.ArgumentNotNull(jobsRunning, nameof(jobsRunning));

            RuleFor(_ => _.FundingStreamId)
                .NotEmpty()
                .WithMessage("You must supply a funding stream id");

            RuleFor(_ => _.FundingPeriodId)
                .NotEmpty()
                .WithMessage("You must supply a funding period id");

            RuleFor(_ => _.FundingLineCode)
                .NotEmpty()
                .WithMessage("You must supply a funding line code");

            RuleFor(_ => _.ProviderId)
                .NotEmpty()
                .WithMessage("You must supply a provider id");

            RuleFor(_ => _.CustomProfileName)
                .NotEmpty()
                .WithMessage("You must supply a custom profile name");

            RuleFor(_ => _.ProfilePeriods)
                .NotEmpty()
                .WithMessage("You must supply at least one profile period");

            RuleFor(_ => _)
                .CustomAsync(async (request, ctx, ct) =>
                {
                    IEnumerable<string> jobDefinitions = new string[] { JobConstants.DefinitionNames.PublishedFundingUndoJob };
                    IEnumerable<string> jobTypesRunning = await jobsRunning.GetJobTypes(request.SpecificationId, jobDefinitions);

                    if (!jobTypesRunning.IsNullOrEmpty())
                    {
                        ctx.AddFailure(nameof(request.SpecificationId), $"There is currently an Undo Publishing job running for specification id '{request.SpecificationId}'");
                    }
                });

            RuleFor(_ => _)
                .CustomAsync(async (request, ctx, ct) =>
                {
                    string providerId = request.ProviderId;
                    string fundingPeriodId = request.FundingPeriodId;
                    string fundingStreamId = request.FundingStreamId;
                    string fundingLineCode = request.FundingLineCode;

                    if (providerId.IsNotNullOrWhitespace() &&
                        fundingStreamId.IsNotNullOrWhitespace() &&
                        fundingPeriodId.IsNotNullOrWhitespace() &&
                        fundingLineCode.IsNotNullOrWhitespace())
                    {
                        string id = $"publishedprovider-{providerId}-{fundingPeriodId}-{fundingStreamId}";

                        PublishedProvider publishedProvider = await resiliencePolicies.PublishedFundingRepository.ExecuteAsync(() =>
                            publishedFunding.GetPublishedProviderById(id, id));

                        if (publishedProvider == null)
                        {
                            ctx.AddFailure("Request", "No matching published provider located");
                        }
                        else if (publishedProvider.Current.FundingLines == null || publishedProvider.Current.FundingLines.All(_ => _.FundingLineCode != fundingLineCode))
                        {
                            ctx.AddFailure(nameof(request.FundingLineCode),
                                $"Did not locate a funding line with code {fundingLineCode}");
                        }
                    }

                    //TODO: check whether the custom name is already in use on the provider??

                    ProfilePeriod[] profilePeriods = (request.ProfilePeriods ?? Array.Empty<ProfilePeriod>()).ToArray();

                    if (profilePeriods.GroupBy(_ => new
                    {
                        _.Year,
                        _.Type,
                        _.TypeValue,
                        _.Occurrence,
                    }).Any(_ => _.Count() > 1))
                    {
                        ctx.AddFailure(nameof(DistributionPeriod.ProfilePeriods),
                            "The profile periods must be for unique occurrences in a funding line");
                    }

                    if (profilePeriods.Any(_ => _.DistributionPeriodId == null || _.DistributionPeriodId.Trim().Length == 0))
                    {
                        ctx.AddFailure(nameof(DistributionPeriod.ProfilePeriods),
                            "The distribution id must be supplied for all profile periods");
                    }
                });

            RuleFor(_ => _)
                .CustomAsync(async (request, ctx, ct) =>
                {
                    string fundingPeriodId = request.FundingPeriodId;
                    string fundingStreamId = request.FundingStreamId;

                    if (fundingStreamId.IsNotNullOrWhitespace() &&
                        fundingPeriodId.IsNotNullOrWhitespace())
                    {
                        FundingConfiguration fundingConfiguration = await policiesService.GetFundingConfiguration(fundingStreamId, fundingPeriodId);
                        if(fundingConfiguration == null || !fundingConfiguration.EnableUserEditableCustomProfiles)
                        {
                            ctx.AddFailure("Request", $"User not allowed to edit custom profiles for funding stream - '{fundingStreamId}' and funding period - '{fundingPeriodId}'");
                        }

                        if(!fundingConfiguration.EnableCarryForward && request.HasCarryOver)
                        {
                            ctx.AddFailure(nameof(request.CarryOver), $"This request contains carry over amount of �{request.CarryOver} but funding configuration has not EnableCarryForward setting enabled for funding stream - '{fundingStreamId}' and funding period - '{fundingPeriodId}'");
                        }
                    }
                });
        }
    }
}