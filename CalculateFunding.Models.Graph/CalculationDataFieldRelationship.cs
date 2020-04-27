using System;
using System.Collections.Generic;
using System.Text;

namespace CalculateFunding.Models.Graph
{
    public class CalculationDataFieldRelationship
    {
        public Calculation Calculation { get; set; }

        public DataField DataField { get; set; }
    }
}
