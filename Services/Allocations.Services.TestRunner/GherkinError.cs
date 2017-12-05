
namespace Allocations.Services.TestRunner
{
    public class GherkinError
    {
        public GherkinError(string errorMessage)
        {
            ErrorMessage = errorMessage;
        }

        public string ErrorMessage { get; private set; }
    }
}