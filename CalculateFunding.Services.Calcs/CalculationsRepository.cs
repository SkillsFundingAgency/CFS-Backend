using CalculateFunding.Models.Calcs;
using CalculateFunding.Repositories.Common.Cosmos;
using CalculateFunding.Services.Calcs.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Calcs
{
    public class CalculationsRepository : ICalculationsRepository
    {
        private readonly CosmosRepository _cosmosRepository;

        public CalculationsRepository(CosmosRepository cosmosRepository)
        {
            _cosmosRepository = cosmosRepository;
        }

        public Task<HttpStatusCode> CreateDraftCalculation(Calculation calculation)
        {
            return  _cosmosRepository.CreateAsync(calculation);
        }

        async public Task<Calculation> GetCalculationById(string calculationId)
        {
            var calculation = await _cosmosRepository.ReadAsync<Calculation>(calculationId);

            if (calculation == null)
                return null;

            return calculation.Content;
        }

	    public Task<IEnumerable<Calculation>> GetCalculationsBySpecificationId(string specificationId)
	    {
		    var calculations = _cosmosRepository.Query<Calculation>().Where(x => x.Specification.Id == specificationId);

		    return Task.FromResult(calculations.AsEnumerable());
	    }

		async public Task<IEnumerable<CalculationVersion>> GetVersionHistory(string calculationId)
        {
            Calculation calculation = await GetCalculationById(calculationId);

            if (calculation == null)
                return null;

            return !calculation.History.IsNullOrEmpty() ? calculation.History : new List<CalculationVersion>();
        }

        async public Task<IEnumerable<CalculationVersion>> GetCalculationVersions(CalculationVersionsCompareModel compareModel)
        {
            Calculation calculation = await GetCalculationById(compareModel.CalculationId);

            if (calculation == null)
                return null;

            IList<CalculationVersion> versions = new List<CalculationVersion>();

            foreach(var version in compareModel.Versions)
            {
                versions.Add(calculation.History.FirstOrDefault(m => m.Version == version));
            }

            return versions;
        }

        public Task<HttpStatusCode> UpdateCalculation(Calculation calculation)
        {
            return _cosmosRepository.UpdateAsync(calculation);
        }

        public Task<IEnumerable<Calculation>> GetAllCalculations()
        {
            var calculations = _cosmosRepository.Query<Calculation>();

            return Task.FromResult(calculations.AsEnumerable());
        }
    }
}
