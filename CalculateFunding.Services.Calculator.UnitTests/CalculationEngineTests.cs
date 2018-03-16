using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Results;
using CalculateFunding.Services.Calculator.Interfaces;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Calculator
{
    [TestClass]
    public class CalculationEngineTests
    {
        const string ProviderId = "12345";

        //[TestMethod]
        //async public Task GenerateAllocations_GivenBuildProject_Runs()
        //{
        //    //Arrange
        //    BuildProject buildProject = CreateBuildProject();

        //    IEnumerable<ProviderSummary> providers = new[]
        //    {
        //        new ProviderSummary{ Id = ProviderId }
        //    };

        //    Func<string, string, Task<IEnumerable<ProviderSourceDataset>>> func = (s, p) =>
        //    {
        //        IEnumerable<ProviderSourceDataset> sources = new[]
        //        {
        //            new ProviderSourceDataset()
        //        };

        //        return Task.FromResult(sources);
        //    };

        //    IAllocationFactory allocationFactory = Substitute.For<IAllocationFactory>();

        //    CalculationEngine calculationEngine = new CalculationEngine(allocationFactory);

        //    //Act
        //    IEnumerable<ProviderResult> results = await calculationEngine.GenerateAllocations(buildProject, providers, func);

        //    //Assert
            
        //}

        static CalculationEngine CreateEngine(IAllocationFactory allocationFactory = null)
        {
            return null;
        }

        static IAllocationFactory CreateAllocationFactory(IAllocationModel allocationModel)
        {
            IAllocationFactory allocationFactory =  Substitute.For<IAllocationFactory>();
            allocationFactory
                .CreateAllocationModel(Arg.Any<Assembly>())
                .Returns(allocationModel);

            return allocationFactory;
        }

        static BuildProject CreateBuildProject()
        {
            BuildProject buildProject = JsonConvert.DeserializeObject<BuildProject>(MockData.SerializedBuildProject());

            return buildProject;
        }

        
    }
}
