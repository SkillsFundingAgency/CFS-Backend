using System.Threading.Tasks;
using CalculateFunding.Models.Specs;
using System;
using System.Linq.Expressions;
using System.Collections.Generic;
using System.Net;
using CalculateFunding.Repositories.Common.Cosmos;

namespace CalculateFunding.Services.Specs.Interfaces
{
    public interface ISpecificationsRepository
    {
        Task<DocumentEntity<Specification>> CreateSpecification(Specification specification);
        Task<FundingPeriod> GetFundingPeriodById(string fundingPeriodId);
        Task<FundingStream> GetFundingStreamById(string fundingStreamId);
        Task<Specification> GetSpecificationById(string specificationId);
        Task<DocumentEntity<Specification>> GetSpecificationDocumentEntityById(string specificationId);
        Task<IEnumerable<Specification>> GetSpecificationsByQuery(Expression<Func<Specification, bool>> query = null);
        Task<IEnumerable<Specification>> GetSpecifications();
        Task<IEnumerable<FundingPeriod>> GetFundingPeriods();
        Task<Specification> GetSpecificationByQuery(Expression<Func<Specification, bool>> query);
        Task<HttpStatusCode> UpdateSpecification(Specification specification);
        Task<Policy> GetPolicyBySpecificationIdAndPolicyName(string specificationId, string policyByName);
        Task<Policy> GetPolicyBySpecificationIdAndPolicyId(string specificationId, string policyId);
        Task<Calculation> GetCalculationBySpecificationIdAndCalculationName(string specificationId, string calculationName);
        Task<Calculation> GetCalculationBySpecificationIdAndCalculationId(string specificationId, string calculationId);
        Task<IEnumerable<T>> GetSpecificationsByRawQuery<T>(string sql);
        Task<IEnumerable<Calculation>> GetCalculationsBySpecificationId(string specificationId);
        Task<HttpStatusCode> SaveFundingStream(FundingStream fundingStream);
        Task<IEnumerable<FundingStream>> GetFundingStreams(Expression<Func<FundingStream, bool>> query = null);
        Task SaveFundingPeriods(IEnumerable<FundingPeriod> fundingPeriods);
        Task<IEnumerable<Specification>> GetApprovedOrUpdatedSpecificationsByFundingPeriodAndFundingStream(string fundingPeriodId, string fundingStreamId);
    }
}