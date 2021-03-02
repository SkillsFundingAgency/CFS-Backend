using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Calcs.ObsoleteItems;
using CalculateFunding.Models.Code;
using CalculateFunding.Services.Calcs.Interfaces;
using Polly;
using Serilog;

namespace CalculateFunding.Services.Calcs
{
    public class CodeContextBuilder : ICodeContextBuilder
    {
        private readonly ILogger _logger;
        private readonly IBuildProjectsService _buildProjects;
        private readonly ISourceCodeService _compiler;
        private readonly ICalculationsRepository _calculations;
        private readonly AsyncPolicy _calculationsResilience;

        public CodeContextBuilder(IBuildProjectsService buildProjects,
            ICalculationsRepository calculations,
            ISourceCodeService compiler,
            ICalcsResiliencePolicies resiliencePolicies,
            ILogger logger)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(buildProjects, nameof(buildProjects));
            Guard.ArgumentNotNull(calculations, nameof(calculations));
            Guard.ArgumentNotNull(compiler, nameof(compiler));
            Guard.ArgumentNotNull(resiliencePolicies?.CalculationsRepository, nameof(resiliencePolicies.CalculationsRepository));

            _buildProjects = buildProjects;
            _calculations = calculations;
            _calculationsResilience = resiliencePolicies.CalculationsRepository;
            _compiler = compiler;
            _logger = logger;
        }

        public async Task<IEnumerable<TypeInformation>> BuildCodeContextForSpecification(string specificationId)
        {
            Guard.IsNullOrWhiteSpace(specificationId, nameof(specificationId));

            _logger.Information("Generating code context for {specificationId}", specificationId);

            BuildProject buildProject = await _buildProjects.GetBuildProjectForSpecificationId(specificationId);

            IEnumerable<Calculation> calculations = await _calculationsResilience.ExecuteAsync(() => _calculations.GetCalculationsBySpecificationId(specificationId));
            IEnumerable<ObsoleteItem> obsoleteItems = await _calculationsResilience.ExecuteAsync(() => _calculations.GetObsoleteItemsForSpecification(specificationId));

            buildProject.Build = _compiler.Compile(buildProject, calculations ?? Enumerable.Empty<Calculation>(), obsoleteItems);

            Guard.ArgumentNotNull(buildProject.Build, nameof(buildProject.Build));

            return await _compiler.GetTypeInformation(buildProject);
        }
    }
}