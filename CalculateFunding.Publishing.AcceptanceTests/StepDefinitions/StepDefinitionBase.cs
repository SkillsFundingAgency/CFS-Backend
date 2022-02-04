using CalculateFunding.Tests.Common.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TechTalk.SpecFlow;

namespace CalculateFunding.Publishing.AcceptanceTests.StepDefinitions
{
    [Binding]
    public abstract class StepDefinitionBase
    {
        protected string GetTestDataContents(string fileAndFolderPath)
        {
            string resourceName = $"CalculateFunding.Publishing.AcceptanceTests.{fileAndFolderPath}";

            string result = typeof(PublishedFundingRepositoryStepDefinitions)
                .Assembly
                .GetEmbeddedResourceFileContents(resourceName);

            return result;
        }
    }
}
