using Serilog;

namespace CalculateFunding.Publishing.AcceptanceTests.Contexts
{
    public class LoggerStepContext : ILoggerStepContext
    {
        public LoggerStepContext(ILogger logger)
        {
            Logger = logger;
        }

        public ILogger Logger { get; }
    }
}
