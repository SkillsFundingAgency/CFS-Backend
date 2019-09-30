using Serilog;

namespace CalculateFunding.Publishing.AcceptanceTests.Contexts
{
    public interface ILoggerStepContext
    {
        ILogger Logger { get; set; }
    }
}
