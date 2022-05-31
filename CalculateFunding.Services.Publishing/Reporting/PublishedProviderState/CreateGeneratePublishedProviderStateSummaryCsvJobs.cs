﻿using CalculateFunding.Common.JobManagement;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Specifications;
using Serilog;

namespace CalculateFunding.Services.Publishing.Reporting.PublishedProviderState
{
    public class CreateGeneratePublishedProviderStateSummaryCsvJobs : JobCreationForSpecification, ICreateGeneratePublishedProviderStateSummaryCsvJobs
    {
        public CreateGeneratePublishedProviderStateSummaryCsvJobs(IJobManagement jobs, ILogger logger)
            : base(jobs,
                logger,
                JobConstants.DefinitionNames.GeneratePublishedProviderStateSummaryCsvJob,
                "New Csv file generation triggered by publishing change")
        {
        }
    }
}