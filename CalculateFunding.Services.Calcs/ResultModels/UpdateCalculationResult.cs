using CalculateFunding.Models.Calcs;
using System;
using System.Collections.Generic;
using System.Text;

namespace CalculateFunding.Services.Calcs.ResultModels
{
    public class UpdateCalculationResult
    {
        public Calculation Calculation { get; set; }

        public BuildProject BuildProject { get; set; }

        public CalculationCurrentVersion CurrentVersion { get; set; }
    }
}
