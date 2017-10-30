using Gherkin.Ast;

namespace Allocations.Gherkin
{
    public class GherkinError
    {
        public GherkinError(string errorMessage, Location location)
        {
            ErrorMessage = errorMessage;
            Location = location;
        }

        public string ErrorMessage { get; private set; }
        public Location Location { get; private set; }
    }
}