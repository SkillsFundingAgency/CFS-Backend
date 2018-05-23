using System;
using System.Collections.Generic;
using System.Text;

namespace CalculateFunding.Models.Specs
{
    public class CalculationCurrentVersion : Calculation
    {
        public string PolicyId { get; set; }

        public string PolicyName { get; set; }
    }
}
