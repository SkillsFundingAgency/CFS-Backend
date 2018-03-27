using CalculateFunding.Models.Calcs;
using Gherkin.Ast;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.TestRunner.Interfaces
{
    public interface IStepParser
    {
        Task Parse(Step step, string stepExpression, GherkinParseResult parseResult, BuildProject buildProject);
    }
}
