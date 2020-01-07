using CalculateFunding.Models.Calcs;

using CalculateFunding.Models.Scenarios;
using System;
using System.Collections.Generic;
using System.Text;

namespace CalculateFunding.Services.TestRunner.Testing
{
    public class TestExecutionModel
    {
        public BuildProject BuildProject { get; set; }

        public IEnumerable<ProviderResult> ProviderResults { get; set; }

    }
}
