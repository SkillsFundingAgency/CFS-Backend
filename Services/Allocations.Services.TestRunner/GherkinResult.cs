using System.Collections.Generic;
using System.Linq;
using Allocations.Models.Results;
using Gherkin.Ast;

namespace Allocations.Services.TestRunner
{
    public class GherkinResult
    {
        public GherkinResult()
        {
            Errors = new List<GherkinError>();
            Dependencies = new List<Dependency>();
        }

        public GherkinResult(string errorMessage, Location location)
        {
            Errors = new List<GherkinError>();
            Dependencies = new List<Dependency>();
            AddError(errorMessage, location);
        }

        public void AddError(string errorMessage, Location location)
        {
            Errors.Add(new GherkinError(errorMessage, location));
        }

        public bool HasErrors => Errors != null && Errors.Any();
        public List<GherkinError> Errors { get; }

        public List<Dependency> Dependencies { get; }

        public bool Abort { get; set; }
    }
}