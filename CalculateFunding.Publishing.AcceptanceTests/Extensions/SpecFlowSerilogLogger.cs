using Microsoft.VisualStudio.TestTools.UnitTesting;
using Serilog;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TechTalk.SpecFlow;

namespace CalculateFunding.Publishing.AcceptanceTests.Extensions
{
    [Binding]
    public class SpecFlowSerilogLogger : ILogger
    {
        private readonly TestContext _ctx;

        public SpecFlowSerilogLogger(TestContext testContext)
        {
            _ctx = testContext;
        }

        public void Write(LogEvent logEvent)
        {
            _ctx.Write($"{logEvent.Level}: {logEvent.RenderMessage()}");
        }
    }
}
