using CalculateFunding.Models.Jobs;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Jobs.Interfaces
{
    public interface IJobDefinitionsRepository
    {
        Task<HttpStatusCode> SaveJobDefinition(JobDefinition definition);

        IEnumerable<JobDefinition> GetJobDefinitions();
    }
}
