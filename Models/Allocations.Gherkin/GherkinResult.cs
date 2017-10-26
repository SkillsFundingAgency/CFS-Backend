using System.Linq;
using Gherkin.Ast;

namespace Allocations.Gherkin
{
    public class GherkinResult
    {
        public GherkinResult()
        {
        }

        public GherkinResult(string errorMessage, Location location)
        {
            Errors = new[] { new GherkinError(errorMessage, location) };
        }

        public bool HasErrors => Errors == null || !Errors.Any();
        public GherkinError[] Errors { get; set; }
    }
}