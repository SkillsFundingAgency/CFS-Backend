
namespace CalculateFunding.Models.Gherkin
{
    public class GherkinError
    {
        public GherkinError(string errorMessage, int? line, int? column)
        {
            ErrorMessage = errorMessage;
            Line = line.HasValue ? line - 2 : null ;
            Column = column;
        }

        public string ErrorMessage { get; }
        public int? Line { get; }
        public int? Column { get; }
    }
}