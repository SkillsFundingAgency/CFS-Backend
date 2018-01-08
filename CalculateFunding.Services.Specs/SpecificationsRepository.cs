using CalculateFunding.Models.Specs;
using CalculateFunding.Repositories.Common.Cosmos;
using CalculateFunding.Services.Specs.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Linq.Expressions;

namespace CalculateFunding.Services.Specs
{
    public class SpecificationsRepository : ISpecificationsRepository
    {
        CosmosRepository _repository;

        public SpecificationsRepository(CosmosRepository cosmosRepository)
        {
            _repository = cosmosRepository;
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

        public async Task<Specification> GetSpecification(string specificationId)
        {
            var documentEntity = await _repository.ReadAsync<Specification>(specificationId);

            return documentEntity.Content;
        }

        public Task<IEnumerable<Specification>> GetSpecificationsByQuery(Expression<Func<Specification, bool>> query)
        {
            var specifications = _repository.Query<Specification>().Where(query);

            return Task.FromResult(specifications.AsEnumerable());
        }

        public Task<IEnumerable<AcademicYear>> GetAcademicYears()
        {
            var academeicYears = _repository.Query<AcademicYear>();

            return Task.FromResult(academeicYears.AsEnumerable());
        }

        public Task<IEnumerable<FundingStream>> GetFundingStreams()
        {
            var fundingStreams = _repository.Query<FundingStream>();

            return Task.FromResult(fundingStreams.AsEnumerable());
        }
    }
}
