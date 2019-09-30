using Serilog;

namespace CalculateFunding.Publishing.AcceptanceTests.Contexts
{
    public class LoggerStepContext : ILoggerStepContext
    {
        public ILogger Logger { get; set; }
    }
}
