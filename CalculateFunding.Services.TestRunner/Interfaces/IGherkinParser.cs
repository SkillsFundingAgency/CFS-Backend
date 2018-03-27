using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Specs;

namespace CalculateFunding.Services.TestRunner
{
    public interface IGherkinParser
    {
        Task<GherkinParseResult> Parse(string gherkin, BuildProject buildProject);
    }
}
