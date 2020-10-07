// ------------------------------------------------------------------------------
//  <auto-generated>
//      This code was generated by SpecFlow (http://www.specflow.org/).
//      SpecFlow Version:3.0.0.0
//      SpecFlow Generator Version:3.0.0.0
// 
//      Changes to this file may cause incorrect behavior and will be lost if
//      the code is regenerated.
//  </auto-generated>
// ------------------------------------------------------------------------------
#region Designer generated code
#pragma warning disable
namespace CalculateFunding.Profiling.GWTs.Features.Mainstream._1920.AdultSkills
{
    using TechTalk.SpecFlow;
    
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("TechTalk.SpecFlow", "3.0.0.0")]
    [System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    [Microsoft.VisualStudio.TestTools.UnitTesting.TestClassAttribute()]
    public partial class AdultSkills1920Feature
    {
        
        private static TechTalk.SpecFlow.ITestRunner testRunner;
        
        private Microsoft.VisualStudio.TestTools.UnitTesting.TestContext _testContext;
        
#line 1 "AdultSkills1920.feature"
#line hidden
        
        public virtual Microsoft.VisualStudio.TestTools.UnitTesting.TestContext TestContext
        {
            get
            {
                return this._testContext;
            }
            set
            {
                this._testContext = value;
            }
        }
        
        [Microsoft.VisualStudio.TestTools.UnitTesting.ClassInitializeAttribute()]
        public static void FeatureSetup(Microsoft.VisualStudio.TestTools.UnitTesting.TestContext testContext)
        {
            testRunner = TechTalk.SpecFlow.TestRunnerManager.GetTestRunner();
            TechTalk.SpecFlow.FeatureInfo featureInfo = new TechTalk.SpecFlow.FeatureInfo(new System.Globalization.CultureInfo("en-US"), "AdultSkills1920", null, ProgrammingLanguage.CSharp, ((string[])(null)));
            testRunner.OnFeatureStart(featureInfo);
        }
        
        [Microsoft.VisualStudio.TestTools.UnitTesting.ClassCleanupAttribute()]
        public static void FeatureTearDown()
        {
            testRunner.OnFeatureEnd();
            testRunner = null;
        }
        
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestInitializeAttribute()]
        public virtual void TestInitialize()
        {
            if (((testRunner.FeatureContext != null) 
                        && (testRunner.FeatureContext.FeatureInfo.Title != "AdultSkills1920")))
            {
                global::CalculateFunding.Profiling.GWTs.Features.Mainstream._1920.AdultSkills.AdultSkills1920Feature.FeatureSetup(null);
            }
        }
        
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestCleanupAttribute()]
        public virtual void ScenarioTearDown()
        {
            testRunner.OnScenarioEnd();
        }
        
        public virtual void ScenarioInitialize(TechTalk.SpecFlow.ScenarioInfo scenarioInfo)
        {
            testRunner.OnScenarioInitialize(scenarioInfo);
            testRunner.ScenarioContext.ScenarioContainer.RegisterInstanceAs<Microsoft.VisualStudio.TestTools.UnitTesting.TestContext>(_testContext);
        }
        
        public virtual void ScenarioStart()
        {
            testRunner.OnScenarioStart();
        }
        
        public virtual void ScenarioCleanup()
        {
            testRunner.CollectScenarioErrors();
        }
        
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestMethodAttribute()]
        [Microsoft.VisualStudio.TestTools.UnitTesting.DescriptionAttribute("A999 AAC1920 Normal Profile")]
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestPropertyAttribute("FeatureTitle", "AdultSkills1920")]
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestCategoryAttribute("ADPIntegrationTest")]
        public virtual void A999AAC1920NormalProfile()
        {
            TechTalk.SpecFlow.ScenarioInfo scenarioInfo = new TechTalk.SpecFlow.ScenarioInfo("A999 AAC1920 Normal Profile", null, new string[] {
                        "ADPIntegrationTest"});
#line 4
this.ScenarioInitialize(scenarioInfo);
            this.ScenarioStart();
#line hidden
            TechTalk.SpecFlow.Table table1 = new TechTalk.SpecFlow.Table(new string[] {
                        "DistributionPeriod",
                        "AllocationValue"});
            table1.AddRow(new string[] {
                        "FY1920",
                        "13626.00"});
            table1.AddRow(new string[] {
                        "FY2021",
                        "8874.00"});
#line 5
testRunner.Given("an ADP request exists for OrgId \'ORG0017981\' IdentifierName \'?\' Identifier \'?\' Al" +
                    "locationStartDate \'01/09/2019\' AllocationEndDate \'01/07/2020\' and FSP \'AAC161919" +
                    "20\' as follows", ((string)(null)), table1, "Given ");
#line 10
testRunner.When("the request is processed", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "When ");
#line hidden
            TechTalk.SpecFlow.Table table2 = new TechTalk.SpecFlow.Table(new string[] {
                        "DistributionPeriod",
                        "AllocationValue"});
            table2.AddRow(new string[] {
                        "FY1920",
                        "13626.00"});
            table2.AddRow(new string[] {
                        "FY2021",
                        "8874.00"});
#line 12
testRunner.Then("an ADP Allocation Profile response is created for OrgId \'ORG0017981\' IdentifierNa" +
                    "me \'?\' Identifier \'?\' AllocationStartDate \'01/09/2019\' AllocationEndDate \'01/07/" +
                    "2020\' and FSP \'AAC16191920\' as follows", ((string)(null)), table2, "Then ");
#line hidden
            TechTalk.SpecFlow.Table table3 = new TechTalk.SpecFlow.Table(new string[] {
                        "Period",
                        "Occurrence",
                        "PeriodYear",
                        "PeriodType",
                        "ProfileValue",
                        "DistributionPeriod"});
            table3.AddRow(new string[] {
                        "September",
                        "1",
                        "2019",
                        "CalendarMonth",
                        "2680.00",
                        "FY1920"});
            table3.AddRow(new string[] {
                        "October",
                        "1",
                        "2019",
                        "CalendarMonth",
                        "2714.00",
                        "FY1920"});
            table3.AddRow(new string[] {
                        "November",
                        "1",
                        "2019",
                        "CalendarMonth",
                        "2112.00",
                        "FY1920"});
            table3.AddRow(new string[] {
                        "December",
                        "1",
                        "2019",
                        "CalendarMonth",
                        "1590.00",
                        "FY1920"});
            table3.AddRow(new string[] {
                        "January",
                        "1",
                        "2020",
                        "CalendarMonth",
                        "1590.00",
                        "FY1920"});
            table3.AddRow(new string[] {
                        "February",
                        "1",
                        "2020",
                        "CalendarMonth",
                        "1476.00",
                        "FY1920"});
            table3.AddRow(new string[] {
                        "March",
                        "1",
                        "2020",
                        "CalendarMonth",
                        "1464.00",
                        "FY1920"});
            table3.AddRow(new string[] {
                        "April",
                        "1",
                        "2020",
                        "CalendarMonth",
                        "2826.00",
                        "FY2021"});
            table3.AddRow(new string[] {
                        "May",
                        "1",
                        "2020",
                        "CalendarMonth",
                        "2610.00",
                        "FY2021"});
            table3.AddRow(new string[] {
                        "June",
                        "1",
                        "2020",
                        "CalendarMonth",
                        "2160.00",
                        "FY2021"});
            table3.AddRow(new string[] {
                        "July",
                        "1",
                        "2020",
                        "CalendarMonth",
                        "1278.00",
                        "FY2021"});
#line 17
testRunner.And("an ADP Delivery Profile response is created which contains the following", ((string)(null)), table3, "And ");
#line 32
testRunner.And("the service returns HTTP status code \'200\'", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "And ");
#line hidden
            this.ScenarioCleanup();
        }
        
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestMethodAttribute()]
        [Microsoft.VisualStudio.TestTools.UnitTesting.DescriptionAttribute("AAC1829 - Periods 2-12")]
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestPropertyAttribute("FeatureTitle", "AdultSkills1920")]
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestCategoryAttribute("ADPIntegrationTest")]
        public virtual void AAC1829_Periods2_12()
        {
            TechTalk.SpecFlow.ScenarioInfo scenarioInfo = new TechTalk.SpecFlow.ScenarioInfo("AAC1829 - Periods 2-12", null, new string[] {
                        "ADPIntegrationTest"});
#line 35
this.ScenarioInitialize(scenarioInfo);
            this.ScenarioStart();
#line hidden
            TechTalk.SpecFlow.Table table4 = new TechTalk.SpecFlow.Table(new string[] {
                        "DistributionPeriod",
                        "AllocationValue"});
            table4.AddRow(new string[] {
                        "FY1920",
                        "24573.00"});
            table4.AddRow(new string[] {
                        "FY2021",
                        "16003.00"});
#line 36
    testRunner.Given("an ADP request exists for OrgId \'ORG0018007\' IdentifierName \'?\' Identifier \'?\' Al" +
                    "locationStartDate \'01/09/2019\' AllocationEndDate \'01/07/2020\' and FSP \'AAC161919" +
                    "20\' as follows", ((string)(null)), table4, "Given ");
#line 41
    testRunner.When("the request is processed", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "When ");
#line hidden
            TechTalk.SpecFlow.Table table5 = new TechTalk.SpecFlow.Table(new string[] {
                        "DistributionPeriod",
                        "AllocationValue"});
            table5.AddRow(new string[] {
                        "FY1920",
                        "24573.00"});
            table5.AddRow(new string[] {
                        "FY2021",
                        "16003.00"});
#line 43
    testRunner.Then("an ADP Allocation Profile response is created for OrgId \'ORG0018007\' IdentifierNa" +
                    "me \'?\' Identifier \'?\' AllocationStartDate \'01/09/2019\' AllocationEndDate \'01/07/" +
                    "2020\' and FSP \'AAC16191920\' as follows", ((string)(null)), table5, "Then ");
#line hidden
            TechTalk.SpecFlow.Table table6 = new TechTalk.SpecFlow.Table(new string[] {
                        "Period",
                        "Occurrence",
                        "PeriodYear",
                        "PeriodType",
                        "ProfileValue",
                        "DistributionPeriod"});
            table6.AddRow(new string[] {
                        "September",
                        "1",
                        "2019",
                        "CalendarMonth",
                        "4833.00",
                        "FY1920"});
            table6.AddRow(new string[] {
                        "October",
                        "1",
                        "2019",
                        "CalendarMonth",
                        "4894.00",
                        "FY1920"});
            table6.AddRow(new string[] {
                        "November",
                        "1",
                        "2019",
                        "CalendarMonth",
                        "3809.00",
                        "FY1920"});
            table6.AddRow(new string[] {
                        "December",
                        "1",
                        "2019",
                        "CalendarMonth",
                        "2867.00",
                        "FY1920"});
            table6.AddRow(new string[] {
                        "January",
                        "1",
                        "2020",
                        "CalendarMonth",
                        "2867.00",
                        "FY1920"});
            table6.AddRow(new string[] {
                        "February",
                        "1",
                        "2020",
                        "CalendarMonth",
                        "2662.00",
                        "FY1920"});
            table6.AddRow(new string[] {
                        "March",
                        "1",
                        "2020",
                        "CalendarMonth",
                        "2641.00",
                        "FY1920"});
            table6.AddRow(new string[] {
                        "April",
                        "1",
                        "2020",
                        "CalendarMonth",
                        "5096.00",
                        "FY2021"});
            table6.AddRow(new string[] {
                        "May",
                        "1",
                        "2020",
                        "CalendarMonth",
                        "4707.00",
                        "FY2021"});
            table6.AddRow(new string[] {
                        "June",
                        "1",
                        "2020",
                        "CalendarMonth",
                        "3895.00",
                        "FY2021"});
            table6.AddRow(new string[] {
                        "July",
                        "1",
                        "2020",
                        "CalendarMonth",
                        "2305.00",
                        "FY2021"});
#line 48
    testRunner.And("an ADP Delivery Profile response is created which contains the following", ((string)(null)), table6, "And ");
#line 63
       testRunner.And("the service returns HTTP status code \'200\'", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "And ");
#line hidden
            this.ScenarioCleanup();
        }
        
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestMethodAttribute()]
        [Microsoft.VisualStudio.TestTools.UnitTesting.DescriptionAttribute("16-18APPS1920 - Short Allocation Months across FY")]
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestPropertyAttribute("FeatureTitle", "AdultSkills1920")]
        [Microsoft.VisualStudio.TestTools.UnitTesting.IgnoreAttribute()]
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestCategoryAttribute("ADPIntegrationTest")]
        public virtual void _16_18APPS1920_ShortAllocationMonthsAcrossFY()
        {
            TechTalk.SpecFlow.ScenarioInfo scenarioInfo = new TechTalk.SpecFlow.ScenarioInfo("16-18APPS1920 - Short Allocation Months across FY", null, new string[] {
                        "ignore",
                        "ADPIntegrationTest"});
#line 67
this.ScenarioInitialize(scenarioInfo);
            this.ScenarioStart();
#line hidden
            TechTalk.SpecFlow.Table table7 = new TechTalk.SpecFlow.Table(new string[] {
                        "DistributionPeriod",
                        "AllocationValue"});
            table7.AddRow(new string[] {
                        "FY1920",
                        "71681.00"});
            table7.AddRow(new string[] {
                        "FY2021",
                        "36191.00"});
#line 68
    testRunner.Given("an ADP request exists for OrgId \'ORG0018001\' IdentifierName \'?\' Identifier \'?\' Al" +
                    "locationStartDate \'01/09/2019\' AllocationEndDate \'01/07/2020\' and FSP \'16-18APPS" +
                    "1920\' as follows", ((string)(null)), table7, "Given ");
#line 73
    testRunner.When("the request is processed", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "When ");
#line hidden
            TechTalk.SpecFlow.Table table8 = new TechTalk.SpecFlow.Table(new string[] {
                        "DistributionPeriod",
                        "AllocationValue"});
            table8.AddRow(new string[] {
                        "FY1920",
                        "71681.00"});
            table8.AddRow(new string[] {
                        "FY2021",
                        "36191.00"});
#line 75
    testRunner.Then("an ADP Allocation Profile response is created for OrgId \'ORG0018001\' IdentifierNa" +
                    "me \'?\' Identifier \'?\' AllocationStartDate \'01/09/2019\' AllocationEndDate \'01/07/" +
                    "2020\' and FSP \'16-18APPS1920\' as follows", ((string)(null)), table8, "Then ");
#line hidden
            TechTalk.SpecFlow.Table table9 = new TechTalk.SpecFlow.Table(new string[] {
                        "Period",
                        "Occurrence",
                        "PeriodYear",
                        "PeriodType",
                        "ProfileValue",
                        "DistributionPeriod"});
            table9.AddRow(new string[] {
                        "September",
                        "1",
                        "2019",
                        "CalendarMonth",
                        "10231.00",
                        "FY1920"});
            table9.AddRow(new string[] {
                        "October",
                        "1",
                        "2019",
                        "CalendarMonth",
                        "10231.00",
                        "FY1920"});
            table9.AddRow(new string[] {
                        "November",
                        "1",
                        "2019",
                        "CalendarMonth",
                        "10231.00",
                        "FY1920"});
            table9.AddRow(new string[] {
                        "December",
                        "1",
                        "2019",
                        "CalendarMonth",
                        "10231.00",
                        "FY1920"});
            table9.AddRow(new string[] {
                        "January",
                        "1",
                        "2020",
                        "CalendarMonth",
                        "10231.00",
                        "FY1920"});
            table9.AddRow(new string[] {
                        "February",
                        "1",
                        "2020",
                        "CalendarMonth",
                        "10231.00",
                        "FY1920"});
            table9.AddRow(new string[] {
                        "March",
                        "1",
                        "2020",
                        "CalendarMonth",
                        "10295.00",
                        "FY1920"});
            table9.AddRow(new string[] {
                        "April",
                        "1",
                        "2020",
                        "CalendarMonth",
                        "9061.00",
                        "FY2021"});
            table9.AddRow(new string[] {
                        "May",
                        "1",
                        "2020",
                        "CalendarMonth",
                        "9061.00",
                        "FY2021"});
            table9.AddRow(new string[] {
                        "June",
                        "1",
                        "2020",
                        "CalendarMonth",
                        "9061.00",
                        "FY2021"});
            table9.AddRow(new string[] {
                        "July",
                        "1",
                        "2020",
                        "CalendarMonth",
                        "9008.00",
                        "FY2021"});
#line 80
    testRunner.And("an ADP Delivery Profile response is created which contains the following", ((string)(null)), table9, "And ");
#line 95
       testRunner.And("the service returns HTTP status code \'200\'", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "And ");
#line hidden
            this.ScenarioCleanup();
        }
        
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestMethodAttribute()]
        [Microsoft.VisualStudio.TestTools.UnitTesting.DescriptionAttribute("16-18APPS1920 - Short Allocation Different Months across FY")]
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestPropertyAttribute("FeatureTitle", "AdultSkills1920")]
        [Microsoft.VisualStudio.TestTools.UnitTesting.IgnoreAttribute()]
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestCategoryAttribute("ADPIntegrationTest")]
        public virtual void _16_18APPS1920_ShortAllocationDifferentMonthsAcrossFY()
        {
            TechTalk.SpecFlow.ScenarioInfo scenarioInfo = new TechTalk.SpecFlow.ScenarioInfo("16-18APPS1920 - Short Allocation Different Months across FY", null, new string[] {
                        "ignore",
                        "ADPIntegrationTest"});
#line 99
this.ScenarioInitialize(scenarioInfo);
            this.ScenarioStart();
#line hidden
            TechTalk.SpecFlow.Table table10 = new TechTalk.SpecFlow.Table(new string[] {
                        "DistributionPeriod",
                        "AllocationValue"});
            table10.AddRow(new string[] {
                        "FY1920",
                        "52418.00"});
            table10.AddRow(new string[] {
                        "FY2021",
                        "26465.00"});
#line 100
    testRunner.Given("an ADP request exists for OrgId \'ORG0018002\' IdentifierName \'?\' Identifier \'?\' Al" +
                    "locationStartDate \'01/09/2019\' AllocationEndDate \'01/07/2020\' and FSP \'16-18APPS" +
                    "1920\' as follows", ((string)(null)), table10, "Given ");
#line 106
    testRunner.When("the request is processed", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "When ");
#line hidden
            TechTalk.SpecFlow.Table table11 = new TechTalk.SpecFlow.Table(new string[] {
                        "DistributionPeriod",
                        "AllocationValue"});
            table11.AddRow(new string[] {
                        "FY1920",
                        "52418.00"});
            table11.AddRow(new string[] {
                        "FY2021",
                        "26465.00"});
#line 108
    testRunner.Then("an ADP Allocation Profile response is created for OrgId \'ORG0018002\' IdentifierNa" +
                    "me \'?\' Identifier \'?\' AllocationStartDate \'01/09/2019\' AllocationEndDate \'01/07/" +
                    "2020\' and FSP \'16-18APPS1920\' as follows", ((string)(null)), table11, "Then ");
#line hidden
            TechTalk.SpecFlow.Table table12 = new TechTalk.SpecFlow.Table(new string[] {
                        "Period",
                        "Occurrence",
                        "PeriodYear",
                        "PeriodType",
                        "ProfileValue",
                        "DistributionPeriod"});
            table12.AddRow(new string[] {
                        "September",
                        "1",
                        "2019",
                        "CalendarMonth",
                        "7482.00",
                        "FY1920"});
            table12.AddRow(new string[] {
                        "October",
                        "1",
                        "2019",
                        "CalendarMonth",
                        "7482.00",
                        "FY1920"});
            table12.AddRow(new string[] {
                        "November",
                        "1",
                        "2019",
                        "CalendarMonth",
                        "7482.00",
                        "FY1920"});
            table12.AddRow(new string[] {
                        "December",
                        "1",
                        "2019",
                        "CalendarMonth",
                        "7482.00",
                        "FY1920"});
            table12.AddRow(new string[] {
                        "January",
                        "1",
                        "2020",
                        "CalendarMonth",
                        "7482.00",
                        "FY1920"});
            table12.AddRow(new string[] {
                        "February",
                        "1",
                        "2020",
                        "CalendarMonth",
                        "7482.00",
                        "FY1920"});
            table12.AddRow(new string[] {
                        "March",
                        "1",
                        "2020",
                        "CalendarMonth",
                        "7526.00",
                        "FY1920"});
            table12.AddRow(new string[] {
                        "April",
                        "1",
                        "2020",
                        "CalendarMonth",
                        "6626.00",
                        "FY2021"});
            table12.AddRow(new string[] {
                        "May",
                        "1",
                        "2020",
                        "CalendarMonth",
                        "6626.00",
                        "FY2021"});
            table12.AddRow(new string[] {
                        "June",
                        "1",
                        "2020",
                        "CalendarMonth",
                        "6626.00",
                        "FY2021"});
            table12.AddRow(new string[] {
                        "July",
                        "1",
                        "2020",
                        "CalendarMonth",
                        "6587.00",
                        "FY2021"});
#line 113
    testRunner.And("an ADP Delivery Profile response is created which contains the following", ((string)(null)), table12, "And ");
#line 128
       testRunner.And("the service returns HTTP status code \'200\'", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "And ");
#line hidden
            this.ScenarioCleanup();
        }
        
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestMethodAttribute()]
        [Microsoft.VisualStudio.TestTools.UnitTesting.DescriptionAttribute("16-18APPS1920 - Periods 9-12 one FY")]
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestPropertyAttribute("FeatureTitle", "AdultSkills1920")]
        [Microsoft.VisualStudio.TestTools.UnitTesting.IgnoreAttribute()]
        public virtual void _16_18APPS1920_Periods9_12OneFY()
        {
            TechTalk.SpecFlow.ScenarioInfo scenarioInfo = new TechTalk.SpecFlow.ScenarioInfo("16-18APPS1920 - Periods 9-12 one FY", null, new string[] {
                        "ignore"});
#line 132
this.ScenarioInitialize(scenarioInfo);
            this.ScenarioStart();
#line hidden
            TechTalk.SpecFlow.Table table13 = new TechTalk.SpecFlow.Table(new string[] {
                        "DistributionPeriod",
                        "AllocationValue"});
            table13.AddRow(new string[] {
                        "FY2021",
                        "31646.00"});
#line 133
    testRunner.Given("an ADP request exists for OrgId \'ORG0018003\' IdentifierName \'?\' Identifier \'?\' Al" +
                    "locationStartDate \'01/04/2020\' AllocationEndDate \'01/07/2020\' and FSP \'16-18APPS" +
                    "1920\' as follows", ((string)(null)), table13, "Given ");
#line 137
    testRunner.When("the request is processed", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "When ");
#line hidden
            TechTalk.SpecFlow.Table table14 = new TechTalk.SpecFlow.Table(new string[] {
                        "DistributionPeriod",
                        "AllocationValue"});
            table14.AddRow(new string[] {
                        "FY2021",
                        "31646.00"});
#line 139
    testRunner.Then("an ADP Allocation Profile response is created for OrgId \'ORG0018003\' IdentifierNa" +
                    "me \'?\' Identifier \'?\' AllocationStartDate \'01/04/2020\' AllocationEndDate \'01/07/" +
                    "2020\' and FSP \'16-18APPS1920\' as follows", ((string)(null)), table14, "Then ");
#line hidden
            TechTalk.SpecFlow.Table table15 = new TechTalk.SpecFlow.Table(new string[] {
                        "Period",
                        "Occurrence",
                        "PeriodYear",
                        "PeriodType",
                        "ProfileValue",
                        "DistributionPeriod"});
            table15.AddRow(new string[] {
                        "April",
                        "1",
                        "2020",
                        "CalendarMonth",
                        "7923.00",
                        "FY2021"});
            table15.AddRow(new string[] {
                        "May",
                        "1",
                        "2020",
                        "CalendarMonth",
                        "7923.00",
                        "FY2021"});
            table15.AddRow(new string[] {
                        "June",
                        "1",
                        "2020",
                        "CalendarMonth",
                        "7923.00",
                        "FY2021"});
            table15.AddRow(new string[] {
                        "July",
                        "1",
                        "2020",
                        "CalendarMonth",
                        "7877.00",
                        "FY2021"});
#line 143
    testRunner.And("an ADP Delivery Profile response is created which contains the following", ((string)(null)), table15, "And ");
#line 151
       testRunner.And("the service returns HTTP status code \'200\'", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "And ");
#line hidden
            this.ScenarioCleanup();
        }
    }
}
#pragma warning restore
#endregion
