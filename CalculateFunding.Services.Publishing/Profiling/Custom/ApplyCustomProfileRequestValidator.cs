using System;
using System.Linq;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;
using FluentValidation;

namespace CalculateFunding.Services.Publishing.Profiling.Custom
{
    public class ApplyCustomProfileRequestValidator : AbstractValidator<ApplyCustomProfileRequest>
    {
        public ApplyCustomProfileRequestValidator(IPublishedFundingRepository publishedFunding,
            IPublishingResiliencePolicies resiliencePolicies)
        {
            Guard.ArgumentNotNull(publishedFunding, nameof(publishedFunding));
            Guard.ArgumentNotNull(resiliencePolicies?.PublishedFundingRepository, nameof(resiliencePolicies.PublishedFundingRepository));

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
                    PublishedProvider publishedProvider = null;

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

                        publishedProvider = await resiliencePolicies.PublishedFundingRepository.ExecuteAsync(() =>
                            publishedFunding.GetPublishedProviderById(id, id));

                        if (publishedProvider == null)
                        {
                            ctx.AddFailure("Request", "No matching published provider located");
                        }
                        else if (publishedProvider.Current.FundingLines.All(_ => _.FundingLineCode != fundingLineCode))
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
                        _.DistributionPeriodId,
                    }).Any(_ => _.Count() > 1))
                    {
                        ctx.AddFailure(nameof(DistributionPeriod.ProfilePeriods),
                            "The profile periods must be for unique occurrences in a funding line");
                    }

                    if (profilePeriods.Where(_ => _.DistributionPeriodId != null && _.DistributionPeriodId.Trim().Length > 0)
                            .GroupBy(_ => _.DistributionPeriodId).Count() > 1)
                    {
                        ctx.AddFailure(nameof(DistributionPeriod.ProfilePeriods),
                            "The profile periods must be specified for one distribution period only");
                    }

                    if (profilePeriods.Any(_ => _.DistributionPeriodId == null || _.DistributionPeriodId.Trim().Length == 0))
                    {
                        ctx.AddFailure(nameof(DistributionPeriod.ProfilePeriods),
                            "The distribution id must be supplied for all profile periods");
                    }
                });
        }
    }
}