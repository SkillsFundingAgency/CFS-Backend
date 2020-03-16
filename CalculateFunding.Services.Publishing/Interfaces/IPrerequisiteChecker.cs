using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Models.Publishing;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IPrerequisiteChecker
    {
        Task PerformChecks<TSpecification>(TSpecification prereqObject, string jobId, IEnumerable<PublishedProvider> publishedProviders = null);

        bool IsCheckerType(PrerequisiteCheckerType type);
    }
}
