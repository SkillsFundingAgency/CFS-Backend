using CalculateFunding.Common.Utility;
using CalculateFunding.Services.CalcEngine.Interfaces;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Extensions;
using Polly;
using Serilog;
using System;
using System.IO;
using System.Threading.Tasks;

namespace CalculateFunding.Services.CalcEngine
{
    public class AssemblyService : IAssemblyService
    {
        private readonly ILogger _logger;
        private readonly ISpecificationAssemblyProvider _specificationAssemblies;
        private readonly ICalculationsRepository _calculationsRepository;
        private readonly AsyncPolicy _calculationsApiClientPolicy;

        public AssemblyService(
            ILogger logger,
            ISpecificationAssemblyProvider specificationAssemblies,
            ICalculatorResiliencePolicies resiliencePolicies,
            ICalculationsRepository calculationsRepository)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(specificationAssemblies, nameof(specificationAssemblies));
            Guard.ArgumentNotNull(resiliencePolicies, nameof(resiliencePolicies));
            Guard.ArgumentNotNull(resiliencePolicies.CalculationsApiClient, nameof(resiliencePolicies.CalculationsApiClient));
            Guard.ArgumentNotNull(calculationsRepository, nameof(calculationsRepository));

            _logger = logger;
            _specificationAssemblies = specificationAssemblies;
            _calculationsRepository = calculationsRepository;
            _calculationsApiClientPolicy = resiliencePolicies.CalculationsApiClient;
        }
        
        public async Task<byte[]> GetAssemblyForSpecification(string specificationId, string etag)
        {
            if (etag.IsNotNullOrWhitespace())
            {
                Stream cachedAssembly = await _specificationAssemblies.GetAssembly(specificationId, etag);

                if (cachedAssembly != null)
                {
                    return cachedAssembly.ReadAllBytes();
                }
            }

            byte[] assembly = 
                await _calculationsApiClientPolicy.ExecuteAsync(() => 
                    _calculationsRepository.GetAssemblyBySpecificationId(specificationId));

            if (assembly == null)
            {
                string error = $"Failed to get assembly for specification Id '{specificationId}'";

                _logger.Error(error);

                throw new RetriableException(error);
            }

            await _specificationAssemblies.SetAssembly(specificationId, new MemoryStream(assembly));


            return assembly;
        }
    }
}
