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

            RuleFor(_ => _.ProviderId)
                .NotEmpty()
                .WithMessage("You must supply a provider id");

            RuleFor(_ => _.CustomProfileName)
                .NotEmpty()
                .WithMessage("You must supply a custom profile name");

            RuleFor(_ => _.ProfileOverrides)
                .NotEmpty()
                .WithMessage("You must supply at least one set of profile overrides");

            RuleFor(_ => _)
                .CustomAsync(async (request, ctx, ct) =>
                {
                    PublishedProvider publishedProvider = null;

                    string providerId = request.ProviderId;
                    string fundingPeriodId = request.FundingPeriodId;
                    string fundingStreamId = request.FundingStreamId;

                    if (providerId.IsNotNullOrWhitespace() &&
                        fundingStreamId.IsNotNullOrWhitespace() &&
                        fundingPeriodId.IsNotNullOrWhitespace())
                    {
                        string id = $"publishedprovider-{providerId}-{fundingPeriodId}-{fundingStreamId}";

                        publishedProvider = await resiliencePolicies.PublishedFundingRepository.ExecuteAsync(() =>
                            publishedFunding.GetPublishedProviderById(id, id));

                        if (publishedProvider == null)
                        {
                            ctx.AddFailure("Request", "No matching published provider located");    
                        }
                    }

                    //TODO: check whether the custom name is already in use on the provider??
                    
                    foreach (FundingLineProfileOverrides fundingLineOverrides in request.ProfileOverrides ?? Array.Empty<FundingLineProfileOverrides>())
                    {
                        if (publishedProvider != null)
                        {
                            string fundingLineCode = fundingLineOverrides.FundingLineCode;

                            if (publishedProvider.Current.FundingLines.All(_ => _.FundingLineCode != fundingLineCode))
                            {
                                ctx.AddFailure(nameof(FundingLineProfileOverrides.FundingLineCode),
                                    $"Did not locate a funding line with code {fundingLineCode}");
                            }
                        }

                        ProfilePeriod[] profilePeriods = fundingLineOverrides.DistributionPeriods?.SelectMany(_ => _.ProfilePeriods ?? Array.Empty<ProfilePeriod>()).ToArray() ??
                                                         Array.Empty<ProfilePeriod>();

                        if (profilePeriods.Any() == false)
                        {
                            ctx.AddFailure(nameof(DistributionPeriod.ProfilePeriods),
                                "The funding line overrides must contain at least one profile period");
                        }

                        if (profilePeriods.GroupBy(_ => new
                        {
                            _.Year,
                            _.Type,
                            _.TypeValue,
                            _.Occurrence
                        }).Any(_ => _.Count() > 1))
                        {
                            ctx.AddFailure(nameof(DistributionPeriod.ProfilePeriods),
                                "The profile periods must be for unique occurrences in a funding line");
                        }
                    }
                });
        }
    }
}