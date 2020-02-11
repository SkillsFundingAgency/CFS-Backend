using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.Graph;
using CalculateFunding.Common.Graph.Interfaces;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Graph;
using CalculateFunding.Services.Graph.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Services.Graph
{
    public class GraphService : IGraphService
    {
        private ICalculationRepository _calcRepository;

        public GraphService(ICalculationRepository calcRepository)
        {
            Guard.ArgumentNotNull(calcRepository, nameof(calcRepository));

            _calcRepository = calcRepository;
        }

        public async Task<IActionResult> DeleteCalculation(string calculationId)
        {
            await _calcRepository.DeleteCalculation(calculationId);

            return new OkResult();
        }

        public async Task<IActionResult> SaveCalculations(IEnumerable<Calculation> calculations)
        {
            await _calcRepository.SaveCalculations(calculations.ToList());

            return new OkResult();
        }
    }
}
