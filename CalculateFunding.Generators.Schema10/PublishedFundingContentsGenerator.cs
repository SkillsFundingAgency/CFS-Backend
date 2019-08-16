using System.Linq;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Core.Extensions;

namespace CalculateFunding.Generators.Schema10
{
    public class PublishedFundingContentsGenerator : IPublishedFundingContentsGenerator
    {
        public string GenerateContents(PublishedFundingVersion publishedFundingVersion)
        {
            Guard.ArgumentNotNull(publishedFundingVersion, nameof(publishedFundingVersion));
            
            dynamic contents = new
            {
                TemplateVersion = publishedFundingVersion.SchemaVersion,
                Id = publishedFundingVersion.FundingId,
                FundingVersion = publishedFundingVersion.Version,
                Status = publishedFundingVersion.Status.ToString(),
                FundingStream = new
                {
                    Code = publishedFundingVersion.FundingStreamId,
                    Name = publishedFundingVersion.FundingStreamName
                },
                FundingPeriod = new
                {
                    publishedFundingVersion.FundingPeriod.Id,
                    publishedFundingVersion.FundingPeriod.Period,
                    publishedFundingVersion.FundingPeriod.Name,
                    Type = publishedFundingVersion.FundingPeriod.Type.ToString(),
                    publishedFundingVersion.FundingPeriod.StartDate,
                    publishedFundingVersion.FundingPeriod.EndDate
                },
                OrganisationGroup = new
                {
                    GroupTypeCode = publishedFundingVersion.OrganisationGroupTypeCode,
                    GroupTypeIdentifier = publishedFundingVersion.OrganisationGroupTypeIdentifier,
                    IdentifierValue = publishedFundingVersion.OrganisationGroupIdentifierValue,
                    GroupTypeCategory = publishedFundingVersion.OrganisationGroupTypeCategory,
                    Name = publishedFundingVersion.OrganisationGroupName,
                    SearchableName = publishedFundingVersion.OrganisationGroupSearchableName,
                    Identifiers = publishedFundingVersion.OrganisationGroupIdentifiers?.Select(groupTypeIdentifier => new
                    {
                        groupTypeIdentifier.Type,
                        groupTypeIdentifier.Value
                    }).ToArray()
                },
                FundingValue = new
                {
                    TotalValue = publishedFundingVersion.TotalFunding,
                    FundingLines = publishedFundingVersion.FundingLines?.Select(fundingLine => new
                    {
                        fundingLine.Name,
                        fundingLine.FundingLineCode,
                        fundingLine.Value,
                        fundingLine.TemplateLineId,
                        Type = fundingLine.Type.ToString(),
                        DistributionPeriods = fundingLine.DistributionPeriods?.Select(distributionPeriod => new
                        {
                            distributionPeriod.Value,
                            distributionPeriod.DistributionPeriodId,
                            ProfilePeriods = distributionPeriod.ProfilePeriods?.Select(profilePeriod => new
                            {
                                Type = profilePeriod.Type.ToString(),
                                profilePeriod.TypeValue,
                                profilePeriod.Year,
                                profilePeriod.Occurrence,
                                profilePeriod.ProfiledValue,
                                profilePeriod.DistributionPeriodId
                            }).ToArray()
                        }).ToArray()
                    }).ToArray()
                },
                ProviderFundings = publishedFundingVersion.ProviderFundings?.ToArray(),
                publishedFundingVersion.GroupingReason,
                publishedFundingVersion.StatusChangedDate,
                publishedFundingVersion.ExternalPublicationDate,
                publishedFundingVersion.EarliestPaymentAvailableDate
            };

            return ((object)contents).AsJson();
        }
    }
}