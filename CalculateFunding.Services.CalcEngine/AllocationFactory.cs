using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.CalcEngine.Interfaces;
using Serilog;

namespace CalculateFunding.Services.CalcEngine
{
    public class AllocationFactory : IAllocationFactory
    {
        private readonly ILogger _logger;

        public AllocationFactory(ILogger logger)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));

            _logger = logger;
        }

        public IAllocationModel CreateAllocationModel(Assembly assembly)
        {
            return new AllocationModel(assembly, _logger);
        }

    }
}
