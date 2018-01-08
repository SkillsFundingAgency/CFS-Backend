using CalculateFunding.Functions.Common;
using CalculateFunding.Models;
using CalculateFunding.Models.Specs;
using CalculateFunding.Repositories.Common.Cosmos;
using CalculateFunding.Services.Specs.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Specs
{
    public class SpecificationsRepository : ISpecificationsRepository
    {
        CosmosRepository _repository;

        public SpecificationsRepository()
        {
            _repository = ServiceFactory.GetService<CosmosRepository>();
        }

        public Task<AcademicYear> GetAcademicYearById(string academicYearId)
        {
            var academicYear = _repository.Query<AcademicYear>().FirstOrDefault(m => m.Id == academicYearId);

            return Task.FromResult(academicYear);
        }

        public Task<FundingStream> GetFundingStreamById(string fundingStreamId)
        {
            var fundingStream = _repository.Query<FundingStream>().FirstOrDefault(m => m.Id == fundingStreamId);

            return Task.FromResult(fundingStream);

        }

        public Task CreateSpecification(Specification specification)
        {
            return _repository.CreateAsync(specification);
        }
    }
}
