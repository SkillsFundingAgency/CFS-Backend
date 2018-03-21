
namespace CalculateFunding.Services.TestRunner
{
    public class GherkinError
    {
        public GherkinError(string errorMessage, int? line, int? column)
        {
            ErrorMessage = errorMessage;
            Line = line;
            Column = column;
        }

        public string ErrorMessage { get; }
        public int? Line { get; }
        public int? Column { get; }
    }
}