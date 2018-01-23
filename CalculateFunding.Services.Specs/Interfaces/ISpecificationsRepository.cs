using System.Threading.Tasks;
using CalculateFunding.Models.Specs;
using System;
using System.Linq.Expressions;
using System.Collections.Generic;
using System.Net;

namespace CalculateFunding.Services.Specs.Interfaces
{
    public interface ISpecificationsRepository
    {
        Task<HttpStatusCode> CreateSpecification(Specification specification);
        Task<AcademicYear> GetAcademicYearById(string academicYearId);
        Task<FundingStream> GetFundingStreamById(string fundingStreamId);
        Task<Specification> GetSpecificationById(string specificationId);
        Task<IEnumerable<Specification>> GetSpecificationsByQuery(Expression<Func<Specification, bool>> query);
        Task<IEnumerable<AcademicYear>> GetAcademicYears();
        Task<IEnumerable<FundingStream>> GetFundingStreams();
        Task<Specification> GetSpecificationByQuery(Expression<Func<Specification, bool>> query);
        Task<HttpStatusCode> UpdateSpecification(Specification specification);
        Task<AllocationLine> GetAllocationLineById(string lineId);
        Task<IEnumerable<AllocationLine>> GetAllocationLines();
        Task<Policy> GetPolicyBySpecificationIdAndPolicyName(string specificationId, string policyByName);
        Task<Policy> GetPolicyBySpecificationIdAndPolicyId(string specificationId, string policyId);
        Task<Calculation> GetCalculationBySpecificationIdAndCalculationName(string specificationId, string calculationName);
        Task<Calculation> GetCalculationBySpecificationIdAndCalculationId(string specificationId, string calculationId);
    }
}