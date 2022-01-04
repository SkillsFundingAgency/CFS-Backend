using CalculateFunding.Common.ApiClient.Calcs.Models;
using CalculateFunding.Common.ApiClient.DataSets.Models;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.ApiClient.Policies.Models;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Migrations.Specification.Clone.Clones
{
    public interface ITargetApiClient
    {
        Task<SpecificationSummary> CreateSpecification(CreateSpecificationModel createSpecificationModel);

        Task<IDictionary<string, JobSummary>> GetLatestJobsForSpecification(string specificationId, params string[] jobDefinitionIds);

        Task<JobViewModel> GetJobById(string jobId);

        Task<Calculation> CreateCalculation(
            string specificationId, 
            CalculationCreateModel calculationCreateModel,
            bool skipCalcRun,
            bool skipQueueCodeContextCacheUpdate,
            bool overrideCreateModelAuthor);

        Task<Calculation> EditCalculationWithSkipInstruct(string specificationId, string calculationId, CalculationEditModel calculationEditModel);

        Task<Common.ApiClient.Calcs.Models.Job> QueueCodeContextUpdate(string specificationId);

        Task<Common.ApiClient.Calcs.Models.Job> QueueCalculationRun(string specificationId, QueueCalculationRunModel model);

        Task<DefinitionSpecificationRelationship> CreateRelationship(CreateDefinitionSpecificationRelationshipModel createDefinitionSpecificationRelationshipModel);

        Task<IEnumerable<Calculation>> GetCalculationsForSpecification(string specificationId);

        Task<FundingPeriod> GetFundingPeriodById(string fundingPeriodId);

        Task<FundingTemplateContents> GetFundingTemplate(string fundingStreamId, string fundingPeriodId, string templateVersion);
    }
}