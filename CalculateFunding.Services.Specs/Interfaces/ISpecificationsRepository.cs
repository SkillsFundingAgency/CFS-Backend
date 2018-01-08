using System.Threading.Tasks;
using CalculateFunding.Models.Specs;

namespace CalculateFunding.Services.Specs.Interfaces
{
    public interface ISpecificationsRepository
    {
        Task CreateSpecification(Specification specification);
        Task<AcademicYear> GetAcademicYearById(string academicYearId);
        Task<FundingStream> GetFundingStreamById(string fundingStreamId);
    }
}