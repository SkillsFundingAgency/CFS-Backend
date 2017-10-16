using System.Linq;
using TechTalk.SpecFlow;
using TechTalk.SpecFlow.Assist;

namespace Allocations.Models.Framework.Specs
{
    [Binding]
    public class StepDefinitions
    {

        public TestContext TestContext { get; }
        public StepDefinitions(TestContext testContext)
        {
            TestContext = testContext;
        }

        [Given(@"I am using the '(.*)' model")]
        public void GivenIAmUsingTheModel(string modelName)
        {
            TestContext.ModelName = modelName;
        }


        [Given(@"I have the following global variables:")]
        public void GivenIHaveTheFollowingGlobalVariables(Table table)
        { 

        }
        
        [Given(@"I have the following '(.*)' provider dataset:")]
        public void GivenIHaveTheFollowingProviderDataset(string datasetName, Table table)
        {
            var dataset = AllocationFactory.CreateDataset(datasetName); ;
            
            table.FillInstance(dataset);

            TestContext.Datasets.Add(datasetName, dataset);

        }
        
        [Given(@"I have the following provider data:")]
        public void GivenIHaveTheFollowingProviderData(Table table)
        {

        }
        
        [When(@"I calculate the allocations for the provider")]
        public void WhenICalculateTheAllocationsForTheProvider()
        {
            var model =
                AllocationFactory.CreateAllocationModel(TestContext.ModelName);

            foreach (var dataset in TestContext.Datasets)// group by URN
            {
          //      model.Execute(TestContext.Datasets.Values.Select(x => x as IProviderDataset).ToArray(), TestContext.ModelName, TestContext.URN);
            }



        }
        
        [Then(@"the allocation statement should be:")]
        public void ThenTheAllocationsShouldBe(Table table)
        {
           // var statementProperty = TestContext.AllocationModel.GetType().GetProperty("Statement");
           // var statement = statementProperty.GetValue(TestContext.AllocationModel);
            var headers = table.Header.ToArray();
            for (var i = 0; i < headers.Length; i++)
            {

              //  var prop = statementProperty.PropertyType.GetProperty(headers[i]);
                // var val = prop.GetValue(statement) as AllocationValue;

                
            }


        }
    }
}