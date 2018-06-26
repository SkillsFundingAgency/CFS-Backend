using System.Collections.Generic;

namespace CalculateFunding.Models.Calcs
{
    public class CalculationVersionsCompareModel
    {
        public string CalculationId { get; set; }

        public IEnumerable<int> Versions { get; set; }
    }
}
