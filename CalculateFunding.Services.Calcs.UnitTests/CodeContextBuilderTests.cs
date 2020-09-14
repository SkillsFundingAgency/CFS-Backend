using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Code;
using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Services.Calcs.Services;
using CalculateFunding.Tests.Common.Helpers;
using Castle.Components.DictionaryAdapter;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NSubstitute;
using Polly;
using Serilog.Core;

namespace CalculateFunding.Services.Calcs.UnitTests
{
    [TestClass]
    public class CodeContextBuilderTests
    {
        private Mock<IBuildProjectsService> _buildProjects;
        private Mock<ISourceCodeService> _compiler;
        private Mock<ICalculationsRepository> _calculations;

        private CodeContextBuilder _codeContextBuilder;

        [TestInitialize]
        public void SetUp()
        {
            _buildProjects = new Mock<IBuildProjectsService>();
            _compiler = new Mock<ISourceCodeService>();
            _calculations = new Mock<ICalculationsRepository>();
            
            _codeContextBuilder = new CodeContextBuilder(_buildProjects.Object,
                _calculations.Object,
                _compiler.Object,
                new ResiliencePolicies
                {
                    CalculationsRepository = Policy.NoOpAsync()
                }, 
                Logger.None);
        }

        [TestMethod]
        public void BuildCodeContextForSpecificationGuardsAgainstMissingSpecificationId()
        {
            Func<Task<IEnumerable<TypeInformation>>> invocation = () => WhenTheCodeContextIsBuilt(null);

            invocation
                .Should()
                .Throw<ArgumentNullException>()
                .Which
                .ParamName
                .Should()
                .Be("specificationId");
        }

        [TestMethod]
        public async Task BuildCodeContextForSpecificationCompilesCalculationsForSuppliedSpecificationIdAndGetsTheirTypeInformation()
        {
            string specificationId = NewRandomString();
            
            BuildProject expectedBuildProject = NewBuildProject();
            Calculation[] calculations = new[]
            {
                NewCalculation(), NewCalculation()
            };
            TypeInformation[] expectedTypeInformation = new[]
            {
                NewTypeInformation(), NewTypeInformation()
            };
            Build expectedBuild = NewBuild();
            
            GivenTheBuildProject(specificationId, expectedBuildProject);
            AndTheCalculations(specificationId, calculations);
            AndTheBuild(expectedBuildProject, calculations, expectedBuild);
            AndTheCodeContext(expectedBuildProject, expectedBuild, expectedTypeInformation);

            IEnumerable<TypeInformation> actualCodeContext = await WhenTheCodeContextIsBuilt(specificationId);

            actualCodeContext
                .Should()
                .BeEquivalentTo<TypeInformation>(expectedTypeInformation);
        }
        
        [TestMethod]
        public async Task BuildCodeContextForSpecificationCompilesAgainstEmptyCalcsListIfNoneExistForSuppliedSpecificationIdAndGetsTheirTypeInformation()
        {
            string specificationId = NewRandomString();
            
            BuildProject expectedBuildProject = NewBuildProject();
            TypeInformation[] expectedTypeInformation = new[]
            {
                NewTypeInformation(), NewTypeInformation()
            };
            Build expectedBuild = NewBuild();
            
            GivenTheBuildProject(specificationId, expectedBuildProject);
            AndTheCalculations(specificationId, null);
            AndTheBuild(expectedBuildProject, null, expectedBuild);
            AndTheCodeContext(expectedBuildProject, expectedBuild, expectedTypeInformation);

            IEnumerable<TypeInformation> actualCodeContext = await WhenTheCodeContextIsBuilt(specificationId);

            actualCodeContext
                .Should()
                .BeEquivalentTo<TypeInformation>(expectedTypeInformation);
        }

        private void GivenTheBuildProject(string specificationId,
            BuildProject buildProject)
            => _buildProjects.Setup(_ => _.GetBuildProjectForSpecificationId(specificationId))
                .ReturnsAsync(buildProject);

        private void AndTheCalculations(string specificationId,
            Calculation[] calculations)
            => _calculations.Setup(_ => _.GetCalculationsBySpecificationId(specificationId))
                .ReturnsAsync(calculations);

        private void AndTheBuild(BuildProject buildProject,
            Calculation[] calculations,
            Build build)
            => _compiler.Setup(_ => _.Compile(buildProject,
                    It.Is<IEnumerable<Calculation>>(calcs =>
                        calcs.SequenceEqual(calculations ?? Enumerable.Empty<Calculation>())),
                        null))
                .Returns(build);

        private void AndTheCodeContext(BuildProject buildProject,
            Build expectedBuild,
            TypeInformation[] codeContext)
            => _compiler.Setup(_ => _.GetTypeInformation(It.Is<BuildProject>(bp =>
                    ReferenceEquals(bp, buildProject) &&
                    ReferenceEquals(bp.Build, expectedBuild))))
                .ReturnsAsync(codeContext);

        private Build NewBuild() => new Build();

        private BuildProject NewBuildProject() => new BuildProject();
        
        private Calculation NewCalculation() => new CalculationBuilder().Build();
        
        private TypeInformation NewTypeInformation() => new TypeInformation();
        
        private string NewRandomString() => new RandomString();

        private async Task<IEnumerable<TypeInformation>> WhenTheCodeContextIsBuilt(string specification)
            => await _codeContextBuilder.BuildCodeContextForSpecification(specification);

    }
}