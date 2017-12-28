using System.Collections.Generic;
using System.Linq;

namespace CalculateFunding.Services.TestRunner
{
    public class GherkinResult
    {
        public GherkinResult()
        {
            Errors = new List<GherkinError>();
            Dependencies = new List<Dependency>();
        }

        public GherkinResult(string errorMessage)
        {
            Errors = new List<GherkinError>();
            Dependencies = new List<Dependency>();
            AddError(errorMessage);
        }

        public void AddError(string errorMessage)
        {
            Errors.Add(new GherkinError(errorMessage));
        }

        public bool HasErrors => Errors != null && Errors.Any();
        public List<GherkinError> Errors { get; }

        public List<Dependency> Dependencies { get; }

        public bool Abort { get; set; }
    }
}