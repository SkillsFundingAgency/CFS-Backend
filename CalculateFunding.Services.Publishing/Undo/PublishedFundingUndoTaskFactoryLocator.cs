using System;
using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Publishing.Interfaces.Undo;
using Serilog;

namespace CalculateFunding.Services.Publishing.Undo
{
    public class PublishedFundingUndoTaskFactoryLocator : IPublishedFundingUndoTaskFactoryLocator
    {
        private readonly IPublishedFundingUndoTaskFactory[] _factories;
        private readonly ILogger _logger;

        public PublishedFundingUndoTaskFactoryLocator(IEnumerable<IPublishedFundingUndoTaskFactory> factories, 
            ILogger logger) 
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(factories, nameof(factories));

            _logger = logger;
            _factories = factories.ToArray();
        }

        public IPublishedFundingUndoTaskFactory GetTaskFactoryFor(PublishedFundingUndoJobParameters parameters)
        {
            _logger.Information($"Requested PublishedFundingUndoTaskFactory for '{parameters}'");
            
            return _factories.SingleOrDefault(_ => _.IsForJob(parameters)) ?? 
                   throw new ArgumentOutOfRangeException(nameof(parameters), $"No configured task factory for {parameters}");
        }
    }
}