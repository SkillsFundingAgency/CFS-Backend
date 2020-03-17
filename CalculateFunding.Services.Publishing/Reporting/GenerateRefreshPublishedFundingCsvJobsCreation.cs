﻿using CalculateFunding.Common.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.Reporting
{
    public class GenerateRefreshPublishedFundingCsvJobsCreation : BaseGeneratePublishedFundingCsvJobsCreation
    {
        public GenerateRefreshPublishedFundingCsvJobsCreation(
            ICreateGeneratePublishedFundingCsvJobs createGeneratePublishedFundingCsvJobs,
            ICreateGeneratePublishedProviderEstateCsvJobs createGeneratePublishedProviderEstateCsvJob)
            : base(createGeneratePublishedFundingCsvJobs, createGeneratePublishedProviderEstateCsvJob)
        {
        }

        public override async Task CreateJobs(string specificationId, string correlationId, Reference user, IEnumerable<string> fundingLineCodes)
        {
            Guard.IsNullOrWhiteSpace(specificationId, nameof(specificationId));
            Guard.ArgumentNotNull(user, nameof(user));
            Guard.ArgumentNotNull(fundingLineCodes, nameof(fundingLineCodes));

            await CreatePublishedFundingCsvJobs(specificationId, correlationId, user, fundingLineCodes);
            await CreatePublishedProviderEstateCsvJobs(specificationId, correlationId, user);
        }

        public override bool IsForAction(GeneratePublishingCsvJobsCreationAction action)
        {
            return action == GeneratePublishingCsvJobsCreationAction.Refresh;
        }
    }
}
