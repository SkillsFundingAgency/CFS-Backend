using CalculateFunding.Common.JobManagement;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Specifications;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.Reporting
{
    public class CreatePublishingReportsJob : JobCreationForSpecification, ICreatePublishingReportsJob
    {
        public CreatePublishingReportsJob(IJobManagement jobs, ILogger logger)
            : base(jobs,
                logger,
                JobConstants.DefinitionNames.PublishingReportsJob,
                "Parent job of all Csv file generation triggered by user")
        {
        }
    }
}
