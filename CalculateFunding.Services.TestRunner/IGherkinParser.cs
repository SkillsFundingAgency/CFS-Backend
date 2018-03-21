using System;
using System.Collections.Generic;
using System.Text;
using CalculateFunding.Models.Calcs;

namespace CalculateFunding.Services.TestRunner
{
    public interface IGherkinParser
    {
        GherkinParseResult Parse(string invalidGherkin);
    }
}
