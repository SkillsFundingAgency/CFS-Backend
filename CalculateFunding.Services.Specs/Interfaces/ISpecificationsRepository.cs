using System.Threading.Tasks;
using CalculateFunding.Models.Specs;
using System;
using System.Linq.Expressions;
using System.Collections.Generic;

namespace CalculateFunding.Services.Specs.Interfaces
{
    public interface ISpecificationsRepository
    {
        Task CreateSpecification(Specification specification);
        Task<AcademicYear> GetAcademicYearById(string academicYearId);
        Task<FundingStream> GetFundingStreamById(string fundingStreamId);
        Task<Specification> GetSpecification(string specificationId);
        Task<IEnumerable<Specification>> GetSpecificationsByQuery(Expression<Func<Specification, bool>> query);
        Task<IEnumerable<AcademicYear>> GetAcademicYears();
        Task<IEnumerable<FundingStream>> GetFundingStreams();
    }
}